using BotBitcointalk.Domain.DbContext;
using BotSpotify.Domain;
using CommonWrapper;
using CommonWrapper.Domain;
using NLog;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BotSpotify
{
	public partial class Main : Form
	{
		#region ===properties===

		private Logger log = LogManager.GetCurrentClassLogger();

		private const string URL_SPOTIFY = "https://www.spotify.com/";
		private const string URL_DEEZER = "https://www.deezer.com/";

		private bool isCanceled;

		private long? selectedUser;

		private int? selectedColumnIndexUser;
		private int? selectedRowIndexUser;

		private int listen = 0;
		private bool isPause;
		private bool isWasPaused;
		TimeSpan time_work;
		TimeSpan time_pause;
		private bool isPlaying;
		private ChromeDriver driver;
		private object isDriver;

		#endregion

		public Main()
		{
			InitializeComponent();

			Singleton.Main = this;
		}

		private void Main_Load(object sender, EventArgs e)
		{
			this.Text = "BotSpotify 2.17";

			try
			{
				numericUpDownMaxThreads.Value = Properties.Settings.Default.MaxThreads;
				checkBoxIsListenTime.Checked = Properties.Settings.Default.IsListenTime;
				numericUpDownMinutes.Enabled = numericUpDownSeconds.Enabled = checkBoxIsListenTime.Checked;
				numericUpDownMinutes.Value = Properties.Settings.Default.MinutesListen;
				numericUpDownSeconds.Value = Properties.Settings.Default.SecondsListen;
				numericUpDownTimeWork.Value = Properties.Settings.Default.TimeWork;
				numericUpDownTimePause.Value = Properties.Settings.Default.TimePause;
				numericUpDownRefreshPage.Value = Properties.Settings.Default.RefreshTime;

				using (BotContext context = new BotContext())
				{
					List<User> list = context.Users.ToList();
					foreach (User user in list)
					{
						user.Status = "";
					}

					context.SaveChanges();

					initUsers(context.Users.ToList(), context.Listenings.Where(x => x.SiteType == User.SiteTypes.Spotify).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Deezer).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Teedal).Count());
				}

				initListenings(new List<Listening>());
				initUserUrls(new List<UserUrl>());

				hideCaret(textBoxStatus);
			}
			catch (Exception ex)
			{
				log.Error(ex);

				MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void buttonStart_Click(object sender, EventArgs e)
		{
			dismissDrivers();

			time_work = new TimeSpan(0, Properties.Settings.Default.TimeWork, 0);
			timerMain.Start();
			timerWork.Start();

			updateStatus("Прослушивание началось");

			Task.Run(() => doWork());
		}

		private void buttonStop_Click(object sender, EventArgs e)
		{
			isPause = true;
			isWasPaused = true;
			isPlaying = false;
			isCanceled = true;
			timerWork.Stop();
			timerPause.Stop();

			Task.Delay(1000).Wait();

			updateStatus("Завершение работы");
			enable();
		}

		private void checkBoxIsListenTime_CheckedChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.IsListenTime = checkBoxIsListenTime.Checked;
			Properties.Settings.Default.Save();

			numericUpDownMinutes.Enabled = numericUpDownSeconds.Enabled = checkBoxIsListenTime.Checked;
		}

		private void dataGridViewUsers_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			var senderGrid = (DataGridView)sender;
			DataGridViewImageColumn column = null;
			DataGridViewCheckBoxColumn columnCheckBox = null;

			if (e.ColumnIndex != -1)
			{
				column = senderGrid.Columns[e.ColumnIndex] as DataGridViewImageColumn;
				columnCheckBox = senderGrid.Columns[e.ColumnIndex] as DataGridViewCheckBoxColumn;

				selectedColumnIndexUser = e.ColumnIndex;
			}

			if (e.RowIndex != -1)
			{
				try
				{
					selectedRowIndexUser = e.RowIndex;

					selectedUser = long.Parse(senderGrid["Id", e.RowIndex].Value.ToString());

					using (BotContext context = new BotContext())
					{
						User user = context.Users.FirstOrDefault(x => x.Id == selectedUser.Value);

						if (column != null)
						{
							if (column.Name == "authorize")
							{
								switch (user.SiteType)
								{
									case User.SiteTypes.Spotify:
										authorization_Spotify(user.Id);
										break;
									case User.SiteTypes.Deezer:
										authorization_Deezer(user.Id);
										break;
								}
							}
							else if (column.Name == "remove")
							{
								context.Users.Remove(user);
								context.SaveChanges();

								selectedUser = null;

								refreshUsers(context.Users.ToList(), context.Listenings.Where(x => x.SiteType == User.SiteTypes.Spotify).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Deezer).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Teedal).Count());
								refreshListenigs(new List<Listening>());
								RefreshUrls(new List<UserUrl>());
							}
						}
						else if (columnCheckBox != null)
						{
							user.IsWork = !(bool)senderGrid[e.ColumnIndex, e.RowIndex].Value;
							context.SaveChanges();

							refreshUsers(context.Users.ToList(), context.Listenings.Where(x => x.SiteType == User.SiteTypes.Spotify).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Deezer).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Teedal).Count());
						}
						else
						{
							refreshListenigs(user.Listenings.OrderByDescending(x => x.DateEnd).ToList());
							RefreshUrls(user.UserUrls.ToList());
						}
					}
				}
				catch (Exception ex)
				{
					log.Error(ex);

					MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void dataGridViewUrls_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			var senderGrid = (DataGridView)sender;
			DataGridViewImageColumn column = senderGrid.Columns[e.ColumnIndex] as DataGridViewImageColumn;

			if (e.RowIndex != -1)
			{
				try
				{
					long id = long.Parse(senderGrid["Id", e.RowIndex].Value.ToString());

					using (BotContext context = new BotContext())
					{
						Domain.User user = context.Users.FirstOrDefault(x => x.Id == selectedUser.Value);
						UserUrl userUrl = context.UserUrls.FirstOrDefault(x => x.Id == id);

						if (column != null)
						{
							if (column.Name == "remove")
							{
								context.UserUrls.Remove(userUrl);
								context.SaveChanges();

								RefreshUrls(user.UserUrls.ToList());
							}
						}
					}
				}
				catch (Exception ex)
				{
					log.Error(ex);

					MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void dataGridViewListenings_CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			var senderGrid = (DataGridView)sender;
			DataGridViewImageColumn column = senderGrid.Columns[e.ColumnIndex] as DataGridViewImageColumn;

			if (e.ColumnIndex != -1)
			{

			}

			if (e.RowIndex != -1)
			{
				try
				{

				}
				catch (Exception ex)
				{
					log.Error(ex);

					MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void numericUpDownMaxThreads_ValueChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.MaxThreads = (int)numericUpDownMaxThreads.Value;
			Properties.Settings.Default.Save();
		}

		private void numericUpDownMinutest_ValueChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.MinutesListen = (int)numericUpDownMinutes.Value;
			Properties.Settings.Default.Save();
		}

		private void numericUpDownSeconds_ValueChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.SecondsListen = (int)numericUpDownSeconds.Value;
			Properties.Settings.Default.Save();
		}

		private void numericUpDownTimeWork_ValueChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.TimeWork = (int)numericUpDownTimeWork.Value;
			Properties.Settings.Default.Save();
		}

		private void numericUpDownTimePause_ValueChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.TimePause = (int)numericUpDownTimePause.Value;
			Properties.Settings.Default.Save();
		}

		//////*****************************************************************************************************************
		//////***************            driver.Navigate().GoToUrl(url);**************************************************************************************************
		/*private void numericUpDownRefreshPage_ValueChanged(object sender, EventArgs e)
		{

		}*/
		//////*****************************************************************************************************************
		private void numericUpDownRefreshPage_ValueChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.RefreshTime = (int)numericUpDownRefreshPage.Value;
			Properties.Settings.Default.Save();
		}

		private void ToolStripMenuItemAddUserSpotify_Click(object sender, EventArgs e)
		{
			isCanceled = false;

			if (openFileDialogMain.ShowDialog() == DialogResult.OK)
			{
				try
				{
					string text = "";
					using (StreamReader sr = new StreamReader(openFileDialogMain.FileName))
					{
						text = sr.ReadToEnd();
					}

					addUser(text, User.SiteTypes.Spotify);
				}
				catch (Exception ex)
				{
					log.Error(ex);

					MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void ToolStripMenuItemClearUser_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("Вы действительно хотите удалить всех пользователей?", "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
			{
				try
				{
					using (BotContext context = new BotContext())
					{
						context.Users.RemoveRange(context.Users);
						context.SaveChanges();

						refreshUsers(context.Users.ToList(), context.Listenings.Where(x => x.SiteType == User.SiteTypes.Spotify).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Deezer).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Teedal).Count());
					}
				}
				catch (Exception ex)
				{
					log.Error(ex);

					MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void ToolStripMenuItemClearHistory_Click(object sender, EventArgs e)
		{
			if (!selectedUser.HasValue)
			{
				MessageBox.Show("Необходимо выбрать пользователя", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (MessageBox.Show("Вы действительно хотите удалить историю прослушивания для выбранного пользователя?", "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
			{
				try
				{
					using (BotContext context = new BotContext())
					{
						User user = context.Users.FirstOrDefault(x => x.Id == selectedUser.Value);
						context.Listenings.RemoveRange(user.Listenings);
						context.SaveChanges();

						refreshListenigs(new List<Listening>());
					}
				}
				catch (Exception ex)
				{
					log.Error(ex);

					MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void ToolStripMenuItemProxies_Click(object sender, EventArgs e)
		{
			EditProxies dialog = new EditProxies();
			dialog.ShowDialog();
		}

		private void ToolStripMenuItemAddUrl_Click(object sender, EventArgs e)
		{
			if (!selectedUser.HasValue)
			{
				MessageBox.Show("Необходимо выбрать аккаунт", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			EditUrl dialog = new EditUrl(selectedUser.Value);
			dialog.ShowDialog();
		}

		private void ToolStripMenuItemAddSpotify_Click(object sender, EventArgs e)
		{
			isCanceled = false;

			if (openFileDialogMain.ShowDialog() == DialogResult.OK)
			{
				try
				{
					string text = "";
					using (StreamReader sr = new StreamReader(openFileDialogMain.FileName))
					{
						text = sr.ReadToEnd();
					}

					addUser(text, User.SiteTypes.Spotify);
				}
				catch (Exception ex)
				{
					log.Error(ex);

					MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void ToolStripMenuItemAddDeezer_Click(object sender, EventArgs e)
		{
			isCanceled = false;

			if (openFileDialogMain.ShowDialog() == DialogResult.OK)
			{
				try
				{
					string text = "";
					using (StreamReader sr = new StreamReader(openFileDialogMain.FileName))
					{
						text = sr.ReadToEnd();
					}

					addUser(text, User.SiteTypes.Deezer);
				}
				catch (Exception ex)
				{
					log.Error(ex);

					MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void timerMain_Tick(object sender, EventArgs e)
		{
			using (BotContext context = new BotContext())
			{
				refreshUsers(context.Users.ToList(), context.Listenings.Where(x => x.SiteType == User.SiteTypes.Spotify).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Deezer).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Teedal).Count());
			}
		}

		private void timerWork_Tick(object sender, EventArgs e)
		{
			time_work = time_work.Subtract(new TimeSpan(0, 0, 1));

			string text = string.Format("До паузы {0} час. {1} мин. {2} сек.", time_work.Hours, time_work.Minutes, time_work.Seconds);
			updateStatus(text);

			if (time_work.Hours == 0 && time_work.Minutes == 0 && time_work.Seconds == 0)
			{
				isPause = true;

				time_work = new TimeSpan(0, Properties.Settings.Default.TimeWork, 0);
				timerWork.Stop();

				time_pause = new TimeSpan(0, Properties.Settings.Default.TimePause, 0);
				timerPause.Start();

			}
		}

		private void timerPause_Tick(object sender, EventArgs e)
		{
			time_pause = time_pause.Subtract(new TimeSpan(0, 0, 1));

			string text = string.Format("До начала работы {0} час. {1} мин. {2} сек.", time_pause.Hours, time_pause.Minutes, time_pause.Seconds);
			updateStatus(text);

			if (time_pause.Hours == 0 && time_pause.Minutes == 0 && time_pause.Seconds <= 0)
			{
				isPause = false;

				timerPause.Stop();
				timerWork.Start();
				time_pause = new TimeSpan(0, Properties.Settings.Default.TimePause, 0);
			}
		}

		#region ===private===

		private void doWork()
		{
			try
			{
				isCanceled = false;
				isPause = false;
				disable();

				List<Task> tasks = new List<Task>();

				listen = 0;

				using (BotContext context = new BotContext())
				{
					List<User> list = context.Users.Where(x => x.IsAuthorized && x.IsWork).ToList();

					if (!list.Any())
						throw new Exception("Нет авторизированных пользователей");

					int cur = 0;
					foreach (User user in list)
					{
						if (isCanceled)
							break;

						if (user.UserUrls.Any())
						{
							switch (user.SiteType)
							{
								case User.SiteTypes.Spotify:
									tasks.Add(new Task(() => doWorkInside_Spotify(user.Id)));
									break;
								case User.SiteTypes.Deezer:
									tasks.Add(new Task(() => doWorkInside_Deezer(user.Id)));
									break;
							}

							cur++;
						}

						if (cur >= Properties.Settings.Default.MaxThreads)
							break;
					}
				}

				foreach (Task task in tasks)
				{
					if (isCanceled)
						break;

					task.Start();
				}

				while (true)
				{
					int not_complited = tasks.Where(x => x.Status != TaskStatus.RanToCompletion).Count();
					if (not_complited == 0)
						break;

					Task.Delay(1000).Wait();
				}

				updateStatus("Работа завершена");
			}
			catch (Exception ex)
			{
				log.Error(ex);

				//updateStatus(ex: ex);
			}
			finally
			{
				enable();

				timerMain.Stop();
				timerWork.Stop();
				timerPause.Stop();

				using (BotContext context = new BotContext())
				{
					refreshUsers(context.Users.ToList(), context.Listenings.Where(x => x.SiteType == User.SiteTypes.Spotify).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Deezer).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Teedal).Count());
				}
			}
		}

		private void enable()
		{
			buttonStart.Invoke((MethodInvoker)delegate
			{
				buttonStart.Enabled =
				groupBox1.Enabled =
				true;

				buttonStop.Enabled = false;
			});
		}

		private void disable()
		{
			buttonStart.Invoke((MethodInvoker)delegate
			{
				buttonStart.Enabled =
				groupBox1.Enabled =
				false;

				buttonStop.Enabled = true;
			});
		}

		private void updateStatus(string text = "", Exception ex = null)
		{
			textBoxStatus.Invoke((MethodInvoker)delegate
			{
				textBoxStatus.ForeColor = ex != null ? Color.Red : Color.Black;
				textBoxStatus.Text = ex != null ? ex.Message : text;
				textBoxStatus.Select(0, 0);
				hideCaret(textBoxStatus);
			});
		}

		private void updateStatus(BotContext context, User user, string text)
		{
			try
			{
				user.Status = text;
				context.SaveChanges();
			}
			catch (Exception ex)
			{
				log.Error(ex);
			}
		}

		private void saveCookies(IWebDriver driver, BotContext context, User user)
		{
			var cookies = driver.Manage().Cookies.AllCookies;
			foreach (var cookie in cookies)
			{
				MyCookie myCookie = user.Cookies.FirstOrDefault(x => x.Name == cookie.Name);
				if (myCookie == null)
				{
					myCookie = new MyCookie();
					myCookie.Name = cookie.Name;
					myCookie.User = user;
				}

				myCookie.Value = cookie.Value;
				myCookie.Domain = cookie.Domain;
				myCookie.Path = cookie.Path;
				myCookie.Expire = cookie.Expiry.ToString();

				if (myCookie.Id == 0)
					user.Cookies.Add(myCookie);
			}
		}

		private void setCookies(IWebDriver driver, User user)
		{
			driver.Manage().Cookies.DeleteAllCookies();

			foreach (MyCookie myCookie in user.Cookies)
			{
				DateTime expire_temp = DateTime.MinValue;
				DateTime? expire = null;
				DateTime.TryParse(myCookie.Expire, out expire_temp);
				if (expire_temp != DateTime.MinValue)
					expire = expire_temp;

				Cookie cookie = new Cookie(myCookie.Name, myCookie.Value, myCookie.Domain, myCookie.Path, expire);

				//log.Info("Name: " + cookie.Name);
				//log.Info("Value: " + cookie.Value);
				//log.Info("Domain: " + cookie.Domain);
				//log.Info("Path: " + cookie.Path);
				//log.Info("Expiry: " + cookie.Expiry);

				try
				{
					driver.Manage().Cookies.AddCookie(cookie);
				}
				catch (Exception ex)
				{
					log.Error(ex);
				}
			}
		}

		private TimeSpan getTimeSpan_Spotify(string source)
		{
			TimeSpan result = new TimeSpan();

			string[] splits = source.Split(':');
			if (splits.Length > 1)
			{
				int min = int.Parse(splits[0]);
				int sec = int.Parse(splits[1]);
				result = new TimeSpan(0, min, sec);
			}

			return result;
		}

		private int setSaveRow(DataGridView dataGridView)
		{
			int saveRow = 0;

			if (dataGridView.Rows.Count > 0 && dataGridView.FirstDisplayedCell != null)
				saveRow = dataGridView.FirstDisplayedCell.RowIndex;

			return saveRow;
		}

		private void addUser(string text, User.SiteTypes type)
		{
			string[] splits = text.Contains("\r\n") ? text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries) :
															 text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

			using (BotContext context = new BotContext())
			{
				int index = 1;

				foreach (string split in splits)
				{
					if (isCanceled)
						break;

					updateStatus("Добавление пользователей " + index + " из " + splits.Length);

					string[] user_string = split.Split(':');
					if (user_string.Length > 1)
					{
						string login = user_string[0];
						User user = context.Users.FirstOrDefault(x => x.Login == login && x.SiteType == type);
						if (user == null)
						{
							user = new User();
							user.Login = user_string[0];
							user.Password = user_string[1];
							user.SiteType = type;

							context.Users.Add(user);
						}
					}

					index++;
				}

				context.SaveChanges();
				refreshUsers(context.Users.ToList(), context.Listenings.Where(x => x.SiteType == User.SiteTypes.Spotify).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Deezer).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Teedal).Count());
			}
		}

		private string getRandomUrl(Domain.User user)
		{
			string result = String.Empty;

			try
			{
				return user.UserUrls.FirstOrDefault().Value;

				//Random random = new Random(DateTime.Now.Millisecond);

				//List<UserUrl> list = user.UserUrls.ToList();
				//int index = random.Next(0, list.Count);
				//result = list[index].Value;

				//if (user.Listenings.Any() && list.Count > 1)
				//{
				//    string last_url = user.Listenings.LastOrDefault().Url;
				//    while (result == last_url)
				//    {
				//        if (isCanceled)
				//            break;

				//        index = random.Next(0, list.Count);
				//        result = list[index].Value;
				//    }
				//}
			}
			catch (Exception ex)
			{
				log.Error(ex);
			}

			return result;
		}

		private void dismissDrivers()
		{
			var processes = Process.GetProcesses().Where(x => x.ProcessName.Contains("chromedriver") || x.ProcessName.Contains("Console Window Host")).ToList();
			if (processes != null)
			{
				foreach (Process process in processes)
				{
					try
					{
						process.Kill();
					}
					catch (Exception ex)
					{
						log.Error(ex);
					}
				}
			}
		}

		[DllImport("user32.dll")]
		static extern bool HideCaret(System.IntPtr hWnd);
		private void hideCaret(TextBox textBox)
		{
			HideCaret(textBox.Handle);
		}

		#endregion
		//****************************************************************************************************************************
		#region ===Spotify===

		private void doWorkInside_Spotify(long id)
		{
			ChromeDriver driver = null;
			User user = null;

			try
			{
				using (BotContext context = new BotContext())
				{
					// достать юзера 
					user = context.Users.FirstOrDefault(x => x.Id == id);
					if (!user.IsAuthorized)
					{
						updateStatus(context, user, "Не авторизирован");

						return;
					}

					// создать драйвер
					updateStatus(context, user, "Создание драйвера");
					driver = WrapperSelenium.CreateDriverChrome(isHeadless: true,
																isCookies: true,
																site_name: user.Id.ToString(),
																isIgnoreCertificateErrors: true,
																isMaximazed: true);

					int error_count = 0;
					while (!isCanceled || error_count < 10)
					{
						try
						{
							var listenings = user.UserUrls.Select(x => x.Value).ToList();
							// пройтись по всем спискам
							foreach (var url in listenings)
							{
								if (isCanceled)
									break;

								updateStatus(context, user, "Переход на сайт");

								while (true)
								{



									driver.Navigate().GoToUrl(url);


									// проверка авторизации
									checkAuthorization_Spotify(driver, context, user);

									// закрыть модальное
									closeModal_Spotify(driver);

									// проверить ошибку
									string error = isError_Spotify(driver);
									if (!String.IsNullOrEmpty(error))
										throw new Exception(error);
									// запустить песн  и
									play_Spotify(driver, context, user);

									//////*****************************************************************************************************************
									//////*****************************************************************************************************************
									// проигрывание
								  if(playing_Spotify(driver, context, user, url))
									{
										break;
									}

								}
							};
						}
						catch (Exception ex)
						{
							log.Error(ex);

							//updateStatus(context, user, "Ошибка: " + ex.Message);
						}
						finally
						{
							Task.Delay(2000).Wait();
						}
					}
				}
			}
			catch (Exception ex)
			{
				log.Error(ex);
			}
			finally
			{
				driver?.Close();
				driver?.Quit();
			}
		}

		private void authorization_Spotify(long id)
		{
			ChromeDriver driver = null;

			try
			{
				using (BotContext context = new BotContext())
				{
					User user = context.Users.FirstOrDefault(x => x.Id == id);

					updateStatus(context, user, "Создание драйвера");

					//string proxy = getRandomProxy();
					//SeleniumProxy seleniumProxy = !String.IsNullOrEmpty(proxy) ? new SeleniumProxy(proxy) : null;
					driver = WrapperSelenium.CreateDriverChrome(isHeadless: true,
																isCookies: true,
																site_name: user.Id.ToString(),
																isIgnoreCertificateErrors: true,
																isSandBox: false,
																//proxy: seleniumProxy,
																isMaximazed: true);

					setCookies(driver, user);
					driver.Navigate().GoToUrl(URL_SPOTIFY);

					IWebElement login_button = WrapperSelenium.WaitLoadList(driver, xPath: ".//a[contains(@class, 'mh-header-secondary')]", timeToWait: 5).LastOrDefault();
					if (login_button != null)
					{
						login_button.Click();

						IWebElement login_text = driver.FindElements(By.Id("login-username")).FirstOrDefault();
						if (login_text != null)
						{
							login_text.Clear();
							login_text.SendKeys(user.Login);
							Task.Delay(1000).Wait();
						}

						IWebElement password_text = driver.FindElements(By.Id("login-password")).FirstOrDefault();
						if (password_text != null)
						{
							password_text.Clear();
							password_text.SendKeys(user.Password);
							Task.Delay(1000).Wait();
						}

						IWebElement button_login = driver.FindElements(By.Id("login-button")).FirstOrDefault();
						if (button_login != null)
						{
							button_login.Click();
							Task.Delay(1000).Wait();
						}

						if (driver.FindElements(By.ClassName("g-recaptcha")).Count > 0)
						{
							updateStatus(context, user, "Ввод капчи");
							var confirmResult = MessageBox.Show("Решили капчу?", "Капча", MessageBoxButtons.YesNo);
							if (confirmResult == DialogResult.No)
								return;
						}

						login_button = driver.FindElements(By.Id("login-button")).FirstOrDefault();
						if (login_button == null)
						{
							user.IsAuthorized = true;
							saveCookies(driver, context, user);
							updateStatus(context, user, "Авторизация прошла успешно");
						}
						else
							updateStatus(context, user, "Авторизация не пройдена");
					}
					else
					{
						user.IsAuthorized = true;
						updateStatus(context, user, "Авторизирован");
					}

					refreshUsers(context.Users.ToList(), context.Listenings.Where(x => x.SiteType == User.SiteTypes.Spotify).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Deezer).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Teedal).Count());
				}
			}
			catch (Exception ex)
			{
				log.Error(ex.Message);

				//updateStatus(ex: ex);
			}
			finally
			{
				driver?.Close();
				driver?.Quit();
			}
		}

		private void checkAuthorization_Spotify(ChromeDriver driver, BotContext context, Domain.User user)
		{
			updateStatus(context, user, "Проверка авторизации");

			IWebElement button_login = WrapperSelenium.WaitLoadList(driver, xPath: ".//a[contains(@class, 'mh-header-secondary')]", timeToWait: 2).LastOrDefault();
			if (button_login != null)
			{
				user.IsAuthorized = button_login == null;

				context.SaveChanges();

				throw new Exception("Не авторизирован");
			}
		}

		private void closeModal_Spotify(ChromeDriver driver)
		{
			IWebElement close = driver.FindElements(By.ClassName("autoplay-modal__close-button")).FirstOrDefault();
			if (close != null && close.Displayed && close.Enabled)
				close.Click();
		}

		private string isError_Spotify(ChromeDriver driver)
		{
			string result = String.Empty;

			try
			{
				IWebElement error = driver.FindElements(By.ClassName("error")).FirstOrDefault();
				if (error != null && error.Displayed && error.Enabled)
				{
					result = error.Text;
				}
			}
			catch (Exception ex)
			{
				log.Error(ex);
			}

			return result;
		}

		private bool play_Spotify(ChromeDriver driver, BotContext context, Domain.User user)
		{
			bool result = false;

			IWebElement play = getBtnPlay_Spotify(driver);
			if (play != null && play.Enabled && play.Displayed)
			{
				try
				{
					updateStatus(context, user, "Начало прослушивания");
					play.Click();

					result = true;
				}
				catch (Exception ex)
				{
					log.Error(ex);

					WrapperSelenium.JavaScriptClick(driver, play);
					result = true;
				}
			}
			else
			{
				IWebElement pause = WrapperSelenium.WaitLoad(driver, xPath: ".//button[contains(@class, 'control-button spoticon-pause-16 control-button--circled')]", timeToWait: 2);
				if (pause != null && pause.Enabled && pause.Displayed)
					result = true;
			}

			return result;
		}


		//********************************

		private bool playing_Spotify(ChromeDriver driver, BotContext context, Domain.User user, string url)
		{
			bool isWasPaused = false;
			var isPlaying = true;
			var isLastTrack = false;
			var startedAt = DateTime.Now;

			updateStatus(context, user, "Прослушивание трека");

			Listening listening = new Listening();
			listening.User = user;
			listening.SiteType = user.SiteType;
			listening.Url = url;
			listening.DateStart = DateTime.Now;

			// тек и макс треков
			var rows = driver.FindElements(By.XPath(".//div[@data-testid = 'tracklist-row']"));
			var indexTrack = 0;
			var maxTrack = rows.Count;
			foreach (var row in rows)
			{
				
				var pause = row.FindElements(By.XPath(".//button[@aria-label = 'Пауза']"))?.FirstOrDefault();
				if (pause != null)
				{
					var parent = pause.FindElement(By.XPath("./.."))
									  .FindElement(By.XPath("./.."))
									  .FindElement(By.XPath("./.."))
									  .FindElement(By.XPath("./.."))
									  .GetAttribute("aria-rowindex");
                    indexTrack = parent.ParseInt().Value  ;
				}
			}

			IWebElement ts_now = driver.FindElements(By.ClassName("playback-bar__progress-time")).FirstOrDefault();
			IWebElement ts_end = driver.FindElements(By.ClassName("_3a5249d5858e3e9a297d855ad04d4be6-scss")).LastOrDefault();
			if (ts_now != null && ts_end != null)
			{
				TimeSpan time_now = getTimeSpan_Spotify(ts_now.Text);
				TimeSpan time_max = getTimeSpan_Spotify(ts_end.Text);

				int listen_minutes = Properties.Settings.Default.MinutesListen;
				int listen_seconds = Properties.Settings.Default.SecondsListen;

				if (time_max.Minutes < listen_minutes)
					listen_minutes = time_max.Minutes;

				if (time_max.Seconds < listen_seconds)
					listen_seconds = time_max.Seconds;

				while (!isCanceled)
				{
					if (DateTime.Now - startedAt >= TimeSpan.FromMinutes(Properties.Settings.Default.RefreshTime) && Properties.Settings.Default.RefreshTime > 0)
					{
						return false;
					}

					if (isPause)
					{
						updateStatus(context, user, "Пауза");

						if (!isWasPaused)
						{
							pause_Spotify(driver);

							isWasPaused = true;
							isPlaying = false;
						}
						Task.Delay(1000).Wait();
						continue;

					}
					else
					{
						// продолжить
						if (!isPlaying)
						{
							Task.Delay(3000).Wait();

							play_Spotify(driver, context, user);
							isWasPaused = false;
							isPlaying = true;

							return false;
						}
					}

					rows = driver.FindElements(By.XPath(".//div[@data-testid = 'tracklist-row']"));
					indexTrack = 0;
					maxTrack = rows.Count;
					foreach (var row in rows)

					{
						var pause = row.FindElements(By.XPath(".//button[@aria-label = 'Пауза']"))?.FirstOrDefault();
						if (pause != null)
						{
							var parent = pause.FindElement(By.XPath("./.."))
											  .FindElement(By.XPath("./.."))
											  .FindElement(By.XPath("./.."))
											  .FindElement(By.XPath("./.."))
											  .GetAttribute("aria-rowindex");

							indexTrack = parent.ParseInt().Value ;
						}
					}

					// если последний трек
					if (indexTrack >= maxTrack)
					{
                        isLastTrack = true;
					}

					// слушать до конца
					if (time_now.Minutes != 0 &&
						time_now.Seconds != 0 &&
						time_now.Minutes == time_max.Minutes &&
						time_now.Seconds == time_max.Seconds)
					{
						// если пошел по 2у кругу
						if (isLastTrack)
						{
							pause_Spotify(driver);

							break;
						}
					}

					// если пошел по 2у кругу
					if (isLastTrack && indexTrack == 0)
					{
						pause_Spotify(driver);

						break;
					}

					// слушать до определенного момента
					if (Properties.Settings.Default.IsListenTime)
					{
						if (time_now.Minutes >= listen_minutes &&
							time_now.Seconds >= listen_seconds)
						{
							//updateStatus(context, user, "Завершение прослушивания");
							//pause_Spotify(driver);

							//break;
						}
					}

					ts_now = driver.FindElements(By.ClassName("playback-bar__progress-time")).FirstOrDefault(x => !String.IsNullOrEmpty(x.Text));
					time_now = getTimeSpan_Spotify(ts_now.Text);

					ts_end = driver.FindElements(By.ClassName("_3a5249d5858e3e9a297d855ad04d4be6-scss")).LastOrDefault(x => !String.IsNullOrEmpty(x.Text));
					time_max = getTimeSpan_Spotify(ts_end.Text);

					updateStatus(context, user, $"Прослушивание трека {indexTrack} из {maxTrack} | " + time_now.ToString().Substring(3) + " - " + time_max.ToString().Substring(3));

					Task.Delay(500).Wait();
				}

				if (isCanceled)
				{
					updateStatus(context, user, "Отменено пользователем");
					pause_Spotify(driver);
				}
				else
					updateStatus(context, user, "Прослушивание завершено");
			}



			listening.DateEnd = DateTime.Now;

			user.Listenings.Add(listening);

			context.SaveChanges();
			return true;
		}

		private bool pause_Spotify(ChromeDriver driver)
		{
			bool result = false;

			try
			{
				var play = getBtnPlay_Spotify(driver);
				if (play != null && play.Enabled && play.Displayed)
				{
					play.Click();

					result = true;
				}
			}
			catch (Exception ex)
			{
				log.Error(ex);
			}

			return result;

		}

		private void goToStart_Spotify(ChromeDriver driver)
		{
			try
			{
				Task.Delay(2300).Wait();

				IWebElement body = WrapperSelenium.WaitLoad(driver, xPath: ".//div[contains(@class, 'middle-align progress-bar__bg')]", timeToWait: 2);

				if (body != null)
				{
					Actions action = new Actions(driver);
					action.MoveToElement(body, 1, 1)
						  .Click()
						  .Build()
						  .Perform();
				}
				else
				{

				}

				Task.Delay(1500).Wait();
			}
			catch (Exception ex)
			{
				log.Error(ex);
			}
		}

		private IWebElement getBtnPlay_Spotify(ChromeDriver driver)
		{
			IWebElement result = null;

			List<IWebElement> buttons = WrapperSelenium.WaitLoadList(driver, xPath: ".//button[contains(@data-testid, 'play-button')]", timeToWait: 2);
			if (buttons != null)
			{
				result = buttons.LastOrDefault(x => x.Displayed && x.Enabled);
			}

			return result;
		}

		#endregion

		#region ===Deezer===

		private void doWorkInside_Deezer(long id)
		{
			ChromeDriver driver = null;
			User user = null;

			try
			{
				int error_count = 0;
				while (true)
				{
					if (isCanceled || error_count >= 10)
						break;

					if (isPause)
					{
						Task.Delay(1000).Wait();
						continue;
					}

					using (BotContext context = new BotContext())
					{
						try
						{
							user = context.Users.FirstOrDefault(x => x.Id == id);
							string url = getRandomUrl(user);

							if (!user.IsAuthorized)
							{
								updateStatus(context, user, "Не авторизирован");
								break;
							}

							if (driver == null)
							{
								updateStatus(context, user, "Создание драйвера");
								driver = WrapperSelenium.CreateDriverChrome(isHeadless: true,
																			isCookies: true,
																			site_name: user.Id.ToString(),
																			isIgnoreCertificateErrors: true,
																			isMaximazed: true,
																			isDisableNotification: true);

								updateStatus(context, user, "Переход на сайт");
								driver.Navigate().GoToUrl(url);

								// проверка авторизации
								checkAuthorization_Deezer(driver, context, user);
							}
							else
							{
								updateStatus(context, user, "Переход на сайт");

								driver.Navigate().GoToUrl(url);
							}

							// запустить песню
							clickPlay_Deezer(driver, context, user, url);

							// проигрывание
							playing_Deezer(driver, context, user, url);
						}
						catch (Exception ex)
						{
							log.Error(ex);

							//updateStatus(context, user, "Ошибка: " + ex.Message);
						}
						finally
						{
							Task.Delay(2000).Wait();
						}
					}
				}
			}
			catch (Exception ex)
			{
				log.Error(ex);
			}
			finally
			{
				driver?.Close();
				driver?.Quit();
			}
		}

		private void authorization_Deezer(long id)
		{
			ChromeDriver driver = null;

			try
			{
				using (BotContext context = new BotContext())
				{
					User user = context.Users.FirstOrDefault(x => x.Id == id);

					updateStatus(context, user, "Создание драйвера");
					driver = WrapperSelenium.CreateDriverChrome(isHeadless: true,
																isCookies: true,
																site_name: user.Id.ToString(),
																isIgnoreCertificateErrors: true,
																isSandBox: false,
																isMaximazed: true);

					setCookies(driver, user);
					driver.Navigate().GoToUrl(URL_DEEZER);

					IWebElement button_login = WrapperSelenium.WaitLoad(driver, id: "topbar-login-button", timeToWait: 2);
					if (button_login != null)
					{
						button_login.Click();

						IWebElement login_text = driver.FindElements(By.Id("login_mail")).FirstOrDefault();
						if (login_text != null)
						{
							login_text.Clear();
							login_text.SendKeys(user.Login);
							Task.Delay(1000).Wait();
						}

						IWebElement password_text = driver.FindElements(By.Id("login_password")).FirstOrDefault();
						if (password_text != null)
						{
							password_text.Clear();
							password_text.SendKeys(user.Password);
							Task.Delay(1000).Wait();
						}

						IWebElement submit = WrapperSelenium.WaitLoad(driver, id: "login_form_submit", timeToWait: 2);
						//driver.FindElements(By.Id("login_form_submit")).FirstOrDefault();
						if (submit != null && submit.Displayed && submit.Enabled)
						{
							submit.Click();
							Task.Delay(1000).Wait();
						}

						button_login = driver.FindElements(By.Id("login-button")).FirstOrDefault();
						if (button_login == null)
						{
							user.IsAuthorized = true;
							saveCookies(driver, context, user);
							updateStatus(context, user, "Авторизация прошла успешно");
						}
						else
							updateStatus(context, user, "Авторизация не пройдена");
					}
					else
					{
						user.IsAuthorized = true;
						updateStatus(context, user, "Авторизирован");
					}

					refreshUsers(context.Users.ToList(), context.Listenings.Where(x => x.SiteType == User.SiteTypes.Spotify).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Deezer).Count(),
													  context.Listenings.Where(x => x.SiteType == User.SiteTypes.Teedal).Count());
				}
			}
			catch (Exception ex)
			{
				log.Error(ex.Message);

				//updateStatus(ex: ex);
			}
			finally
			{
				driver?.Close();
				driver?.Quit();
			}
		}

		private void checkAuthorization_Deezer(ChromeDriver driver, BotContext context, Domain.User user)
		{
			updateStatus(context, user, "Проверка авторизации");

			IWebElement button_login = WrapperSelenium.WaitLoad(driver, id: "topbar-login-button", timeToWait: 2);
			user.IsAuthorized = button_login == null;
			if (!user.IsAuthorized)
			{
				context.SaveChanges();

				throw new Exception("Не авторизирован");
			}
		}

		private void playing_Deezer(ChromeDriver driver, BotContext context, Domain.User user, string url)
		{
			int index = 0;

			bool isWasPaused = false;

			updateStatus(context, user, "Прослушивание трека");

			Listening listening = new Listening();
			listening.User = user;
			listening.SiteType = user.SiteType;
			listening.Url = url;
			listening.DateStart = DateTime.Now;

			IWebElement progress_handler = driver.FindElements(By.ClassName("slider-track-input")).FirstOrDefault();
			if (progress_handler != null)
			{
				TimeSpan time_now = new TimeSpan();
				TimeSpan time_max = new TimeSpan();
				getTimeSpan_Deezer(ref time_now, ref time_max, progress_handler.GetAttribute("aria-valuenow"), progress_handler.GetAttribute("aria-valuemax"));

				int listen_minutes = Properties.Settings.Default.MinutesListen;
				int listen_seconds = Properties.Settings.Default.SecondsListen;

				if (time_max.Minutes < listen_minutes)
					listen_minutes = time_max.Minutes;

				if (time_max.Seconds < listen_seconds)
					listen_seconds = time_max.Seconds;

				while (true)
				{
					if (isCanceled)
						break;

					try
					{
						driver.SwitchTo().Alert().Dismiss();
					}
					catch (Exception ex)
					{

					}

					if (isPause)
					{
						if (!isWasPaused)
						{
							pause_Deezer(driver);
							isWasPaused = true;
						}

						Task.Delay(1000).Wait();
						continue;
					}
					else if (isWasPaused)
					{
						play_Deezer(driver, context, user);

						isWasPaused = false;
					}

					// слушать до конца
					if (time_now.Minutes != 0 &&
						time_now.Seconds != 0 &&
						time_now.Minutes == time_max.Minutes &&
						time_now.Seconds == time_max.Seconds)
					{
						pause_Spotify(driver);

						break;
					}

					// слушать до определенного момента
					if (Properties.Settings.Default.IsListenTime)
					{
						if (time_now.Minutes >= listen_minutes &&
							time_now.Seconds >= listen_seconds)
						{
							updateStatus(context, user, "Завершение прослушивания");
							pause_Deezer(driver);

							break;
						}
					}

					progress_handler = driver.FindElements(By.ClassName("slider-track-input")).FirstOrDefault();
					getTimeSpan_Deezer(ref time_now, ref time_max, progress_handler.GetAttribute("aria-valuenow"), progress_handler.GetAttribute("aria-valuemax"));

					updateStatus(context, user, "Прослушивание трека " + time_now.ToString().Substring(3) + " - " + time_max.ToString().Substring(3));

					Task.Delay(500).Wait();

					index++;

					if (time_now.Minutes == 0 && time_now.Seconds == 0)
					{
						index++;
						if (index > 10)
							break;
					}
				}

				if (isCanceled)
					updateStatus(context, user, "Отменено пользователем");
				else
					updateStatus(context, user, "Прослушивание завершено");
			}

			listening.DateEnd = DateTime.Now;

			user.Listenings.Add(listening);
			context.SaveChanges();
		}

		private void clickPlay_Deezer(ChromeDriver driver, BotContext context, Domain.User user, string url)
		{
			bool isPlay = play_Deezer(driver, context, user);
			while (!isPlay)
			{
				if (isCanceled)
					break;

				isPlay = play_Deezer(driver, context, user);

				Task.Delay(1000).Wait();
			}
		}

		private bool play_Deezer(ChromeDriver driver, BotContext context, Domain.User user)
		{
			bool result = false;

			IWebElement play = getBtnPlay_Deezer(driver);
			if (play != null && play.Enabled && play.Displayed)
			{
				try
				{
					updateStatus(context, user, "Начало прослушивания");
					play.Click();

					Task.Delay(1000).Wait();

					result = true;
				}
				catch (Exception ex)
				{
					log.Error(ex);

					WrapperSelenium.JavaScriptClick(driver, play);
					result = true;
				}
			}
			else
			{
				IWebElement pause = WrapperSelenium.WaitLoad(driver, xPath: ".//button[contains(@class, 'svg-icon-group-btn is-highlight')]", timeToWait: 2);
				if (pause != null && pause.Enabled && pause.Displayed)
					result = true;
			}

			return result;
		}

		private IWebElement getBtnPlay_Deezer(ChromeDriver driver)
		{
			IWebElement result = WrapperSelenium.WaitLoad(driver, xPath: ".//button[contains(@class, 'svg-icon-group-btn is-highlight')]", timeToWait: 2);

			return result;
		}

		private void getTimeSpan_Deezer(ref TimeSpan now, ref TimeSpan end, string source_now, string source_end)
		{
			string[] minutes = source_now.Split('.');
			int temp_minutes = 0;
			if (int.TryParse(minutes[0], out temp_minutes))
			{
				now = new TimeSpan(0, 0, temp_minutes);
			}

			string[] minutes2 = source_end.Split('.');
			int temp_minutes2 = 0;
			if (int.TryParse(minutes2[0], out temp_minutes2))
			{
				end = new TimeSpan(0, 0, temp_minutes2);
			}
		}

		private bool pause_Deezer(ChromeDriver driver)
		{
			bool result = false;

			try
			{
				IWebElement pause = WrapperSelenium.WaitLoad(driver, xPath: ".//button[contains(@class, 'control control-play')]", timeToWait: 2);
				if (pause != null && pause.Displayed && pause.Enabled && pause.Text == "")
				{
					pause.Click();
					Task.Delay(1000).Wait();
					result = true;
				}
			}
			catch (Exception ex)
			{
				log.Error(ex);
			}

			return result;
		}

		#endregion

		#region ===init===

		private void initUsers(List<User> list, int spotify, int deezer, int teedal)
		{
			//dataGridViewUsers.InitDataGrid();
			dataGridViewUsers.DataSource = list;

			foreach (DataGridViewColumn column in dataGridViewUsers.Columns)
			{
				switch (column.Name)
				{
					case "Login":
						column.HeaderText = "Логин";
						column.Width = 150;
						break;
					case "Password":
						column.HeaderText = "Пароль";
						column.Width = 100;
						break;
					case "SiteType":
						column.HeaderText = "Сайт";
						column.Width = 50;
						break;
					case "IsAuthorizedText":
						column.HeaderText = "Автор.?";
						column.Width = 50;
						break;
					case "Status":
						column.HeaderText = "Статус";
						column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
						break;
					case "IsWork":
						column.HeaderText = "";
						column.Width = 20;
						break;
					default:
						column.Visible = false;
						break;
				}
			}

			dataGridViewUsers.ClearSelection();

			labelSpotify.Text = "Spotify: " + spotify;
			labelDeezer.Text = "Deezer: " + deezer;
			labelTeedal.Text = "Teedal: " + teedal;

			DataGridViewImageColumn colAuthorize = new DataGridViewImageColumn();
			colAuthorize.Name = "authorize";
			colAuthorize.HeaderText = "";
			colAuthorize.Width = 20;
			colAuthorize.Image = Properties.Resources.login;
			dataGridViewUsers.Columns.Add(colAuthorize);

			DataGridViewImageColumn colRemove = new DataGridViewImageColumn();
			colRemove.Name = "remove";
			colRemove.HeaderText = "";
			colRemove.Width = 20;
			colRemove.Image = Properties.Resources.cancel;
			dataGridViewUsers.Columns.Add(colRemove);
		}

		private void initListenings(List<Listening> list)
		{
			list = list.OrderByDescending(x => x.DateEnd).ToList();

			//dataGridViewListenings.InitDataGrid();
			dataGridViewListenings.DataSource = list;

			foreach (DataGridViewColumn column in dataGridViewListenings.Columns)
			{
				switch (column.Name)
				{
					case "Url":
						column.HeaderText = "Ссылка";
						column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
						break;
					case "DateStart":
						column.HeaderText = "Начало";
						column.DefaultCellStyle.Format = "dd.MM.yyyy HH:mm:ss";
						column.Width = 125;
						break;
					case "DateEnd":
						column.HeaderText = "Завершение";
						column.DefaultCellStyle.Format = "dd.MM.yyyy HH:mm:ss";
						column.Width = 125;
						break;
					default:
						column.Visible = false;
						break;
				}
			}

			dataGridViewListenings.ClearSelection();
		}

		private void initUserUrls(List<UserUrl> list)
		{
			//dataGridViewUrls.InitDataGrid();
			dataGridViewUrls.DataSource = list;

			foreach (DataGridViewColumn column in dataGridViewUrls.Columns)
			{
				switch (column.Name)
				{
					case "Value":
						column.HeaderText = "Значение";
						column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
						break;
					default:
						column.Visible = false;
						break;
				}
			}

			DataGridViewImageColumn colRemove = new DataGridViewImageColumn();
			colRemove.Name = "remove";
			colRemove.HeaderText = "";
			colRemove.Width = 20;
			colRemove.Image = Properties.Resources.cancel;
			dataGridViewUrls.Columns.Add(colRemove);
		}

		#endregion

		#region ===refresh===

		private void refreshUsers(List<User> list, int spotify, int deezer, int teedal)
		{
			dataGridViewUsers.Invoke((MethodInvoker)delegate
			{
				try
				{
					int saveRow = setSaveRow(dataGridViewUsers);
					int h = dataGridViewUsers.HorizontalScrollingOffset;

					dataGridViewUsers.DataSource = list;
					dataGridViewUsers.Refresh();

					if (selectedRowIndexUser.HasValue && selectedColumnIndexUser.HasValue &&
						dataGridViewUsers.Columns.Count > selectedColumnIndexUser.Value &&
						dataGridViewUsers.Rows.Count > selectedRowIndexUser.Value)
					{
						dataGridViewUsers.ClearSelection();
						dataGridViewUsers[selectedColumnIndexUser.Value, selectedRowIndexUser.Value].Selected = true;
					}

					if (saveRow != 0 && saveRow < dataGridViewUsers.Rows.Count)
						dataGridViewUsers.FirstDisplayedScrollingRowIndex = saveRow;

					dataGridViewUsers.HorizontalScrollingOffset = h;

					labelSpotify.Text = "Spotify: " + spotify;
					labelDeezer.Text = "Deezer: " + deezer;
					labelTeedal.Text = "Teedal: " + teedal;
				}
				catch (Exception ex)
				{

				}
			});
		}

		private void refreshListenigs(List<Listening> list)
		{
			dataGridViewListenings.Invoke((MethodInvoker)delegate
			{
				try
				{
					dataGridViewListenings.DataSource = list;
					dataGridViewListenings.ClearSelection();
				}
				catch (Exception ex)
				{

				}
			});
		}

		public void RefreshUrls(List<UserUrl> list)
		{
			dataGridViewUrls.Invoke((MethodInvoker)delegate
			{
				try
				{
					dataGridViewUrls.DataSource = list;
					dataGridViewUrls.ClearSelection();
				}
				catch (Exception ex)
				{

				}
			});
		}

		#endregion

	   
	}
}
