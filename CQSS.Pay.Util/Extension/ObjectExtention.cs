using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.Util.Extension
{
    public static class ObjectExtention
    {
        /// <summary>
        /// 设置泛型T的属性的值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="pi"></param>
        /// <param name="value"></param>
        public static void SetPropertyValue<T>(this T t, PropertyInfo pi, object value) where T : class
        {
            if (t == null || pi == null)
                return;

            try
            {
                pi.SetValue(t, Convert.ChangeType(value, pi.PropertyType), null);
            }
            catch
            {
                try
                {
                    if (value.GetType().Equals(typeof(bool)))
                        value = (bool)value ? 1 : 0;

                    if (typeof(System.Enum).IsAssignableFrom(pi.PropertyType))
                        pi.SetValue(t, Enum.Parse(pi.PropertyType, (value ?? "").ToString()), null);
                    else
                        pi.SetValue(t, value, null);
                }
                catch { }
            }
        }
    }
}
