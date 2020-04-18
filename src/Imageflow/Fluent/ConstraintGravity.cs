using System;
using System.Collections.Generic;
using System.Text;

namespace Imageflow.Net.Fluent
{
    public class ConstraintGravity
    {
        /// <summary>
        /// Centers the gravity (the default)
        /// </summary>
        public ConstraintGravity()
        {
            x = 50;
            y = 50;
        }
        public ConstraintGravity(float x_percent, float y_percent)
        {
            x = x_percent;
            y = y_percent;
        }
        float x;
        float y;

        public object ToImageflowDynamic()
        {
            return new
            {
                percentage = new {
                    x,
                    y
                }

            };
        }
    }

}
