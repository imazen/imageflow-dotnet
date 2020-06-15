using System;
using System.Collections.Generic;
using System.Text;

namespace Imageflow.Fluent
{
    public class ConstraintGravity
    {
        /// <summary>
        /// Centers the gravity (the default)
        /// </summary>
        public ConstraintGravity()
        {
            XPercent = 50;
            YPercent = 50;
        }
        public ConstraintGravity(float xPercent, float yPercent)
        {
            XPercent = xPercent;
            YPercent = yPercent;
        }

        public float XPercent { get; }
        public float YPercent { get; }

        public object ToImageflowDynamic()
        {
            return new
            {
                percentage = new {
                    XPercent,
                    YPercent
                }

            };
        }
    }

}
