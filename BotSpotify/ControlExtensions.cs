using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BotSpotify
{
    public static class ControlExtensions
    {
        public static void DoubleBuffered(this Control control, bool enable)
        {
            var doubleBufferPropertyInfo = control.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            doubleBufferPropertyInfo.SetValue(control, enable, null);
        }

        /// <summary>
        /// преобразовать в int?
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static int? ParseInt(this String source)
        {
            int? result = null;

            int temp = 0;

            source = source.Replace(".", ",");
            if (int.TryParse(source, out temp))
                result = temp;

            return result;
        }
    }
}
