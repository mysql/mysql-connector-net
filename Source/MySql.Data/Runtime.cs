using System;

namespace MySql.Web.Security
{
    internal static class Runtime
    {
        private static bool inited;
        private static bool isMono;

        public static bool IsMono
        {
            get
            {
                if (!inited)
                    Init();
                return isMono;
            }
        }

        private static void Init()
        {
            Type t = Type.GetType("Mono.Runtime");
            isMono = t != null;
        }
    }
}
