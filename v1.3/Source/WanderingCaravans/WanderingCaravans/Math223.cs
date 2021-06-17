using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WanderingCaravans
{
    public static class Math223
    {
        public static float CurvePoint(int x, float curveAngle) => x / (curveAngle + x);

        public static float Inverse(float val) => 1f / val;
    }
}