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
            x = 50;
            y = 50;
        }
        public ConstraintGravity(float xPercent, float yPercent)
        {
            x = xPercent;
            y = yPercent;
        }
        readonly float x;
        readonly float y;

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
