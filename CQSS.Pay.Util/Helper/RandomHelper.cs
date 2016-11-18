using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.Util.Helper
{
    /// <summary>
    /// 常用工具包
    /// </summary>
    public class RandomHelper
    {
        /// <summary>
        /// 随机数生成器实例
        /// </summary>
        private static Random _random = null;

        /// <summary>
        /// 生成随机数
        /// </summary>
        /// <param name="minValue">随机数最小值</param>
        /// <param name="maxValue">随机数最大值</param>
        /// <returns></returns>
        public static int CreateRandomValue(int minValue, int maxValue)
        {
            if (_random == null)
            {
                long tick = DateTime.Now.Ticks;
                _random = new Random((int)(tick & 0xffffffffL) | (int)(tick >> 32));
            }

            if (maxValue == int.MaxValue)
                maxValue -= 1;

            int value = _random.Next(minValue, maxValue + 1);
            return value;
        }

        /// <summary>
        /// 生成随机字符串
        /// </summary>
        /// <param name="length">字符串长度</param>
        /// <returns></returns>
        public static string CreateRandomCode(int length)
        {
            string str = "1,2,3,4,5,6,7,8,9,0,a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z,A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z";
            string[] strArr = str.Split(',');
            string code = "";
            for (int i = 0; i < length; i++)
            {
                int index = CreateRandomValue(0, strArr.Length - 1);
                code += strArr[index];
            }
            return code;
        }
    }
}
