/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using System;
using System.Collections.Generic;

namespace fui
{
    public static class FuiCfg
    {
        public static readonly Dictionary<string, Type> Binders = new Dictionary<string, Type>
        {
            {Common.AlertMsg.URL, typeof(Common.AlertMsg)},
            {Common.ToastMsg.URL, typeof(Common.ToastMsg)},
            {Loading.Loading.URL, typeof(Loading.Loading)},
            {Loading.Link.URL, typeof(Loading.Link)},
            {Loading.HotFix.URL, typeof(Loading.HotFix)},
            {Login.Login.URL, typeof(Login.Login)},
        }
        ;
        public static readonly Dictionary<string, string[]> Depends = new Dictionary<string, string[]>
        {
            {"fui.Common.AlertMsg", new []{"Common"}},
            {"fui.Common.ToastMsg", new []{"Common"}},
            {"fui.Loading.Loading", new []{"Loading"}},
            {"fui.Loading.Link", new []{"Loading"}},
            {"fui.Loading.HotFix", new []{"Loading"}},
            {"fui.Login.Login", new []{"Login"}},
        }
        ;
    }
}