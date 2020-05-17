using System;
using System.Linq;

namespace Codebot.Web
{
    public class PageHandler : BasicHandler
    {
        public delegate void WebMethod();

        /// <summary>
        /// Invoked when no default page is found
        /// </summary>
        protected virtual void EmptyPage()
        {
        }

        private bool InvokePageType<T>() where T : PageTypeAttribute
        {
            var pageType = GetType().GetCustomAttribute<T>(true);
            if (pageType != null)
            {
                ContentType = pageType.ContentType;
                Include(pageType.FileName, pageType.IsTemplate);
                return true;
            }
            return false;
        }

        private void InvokeDefaultPage()
        {
            /* For now logging and user authetication have not been ported over
            var logged = GetType().GetCustomAttribute<LoggedAttribute>(true);
            if (logged != null)
                Log.Add(this);
            if ((!IsAuthenticated && InvokePageType<LoginPageAttribute>()) || InvokePageType<DefaultPageAttribute>())
                return; */
            if (InvokePageType<DefaultPageAttribute>())
                return;
            EmptyPage();
        }

        /// <summary>
        /// Invoked when no method is found
        /// </summary>
        protected virtual void EmptyMethod(string methodName)
        {
            InvokeDefaultPage();
        }

        /// <summary>
        /// Provide a chance for desedentdescendents to check if the method is allowed
        /// </summary>
        /// <returns>Return true to allow the method to be invoked</returns>
        /// <param name="methodName">The name of the method to check</param>
        protected virtual bool AllowMethod(string methodName)
        {
            return true;
        }

        protected virtual void OnDeny(string methodName)
        {
        }

        protected virtual void OnAllow(string methodName)
        {
        }

        private bool Deny(MethodPageAttribute a)
        {
            var deny = a.Deny;
            if (String.IsNullOrEmpty(deny))
                return false;
            var list = deny.Split(",").Select(s => s.Trim().ToLower());
            var user = Context.User;
            if (user == null)
                return list.Contains("anonymous");
            foreach (var role in list)
                if (user.IsInRole(role))
                    return true;
            return false;
        }

        private bool Allow(MethodPageAttribute a)
        {
            var allow = a.Allow;
            if (String.IsNullOrEmpty(allow))
                return true;
            var list = allow.Split(",").Select(s => s.Trim().ToLower());
            var user = Context.User;
            if (user == null)
                return list.Contains("anonymous");
            foreach (var role in list)
                if (user.IsInRole(role))
                    return true;
            return false;
        }

        private void InvokeMethod(string methodName)
        {
            foreach (var method in GetType().GetMethods())
            {
                var attribute = method.GetCustomAttributes<MethodPageAttribute>(true)
                    .FirstOrDefault(a => a.MethodName.ToLower() == methodName);
                if (attribute != null)
                {
                    if (Deny(attribute))
                        OnDeny(methodName);
                    else if (Allow(attribute))
                    {
                        OnAllow(methodName);
                        ContentType = attribute.ContentType;
                        var pageMethod = (WebMethod)Delegate.CreateDelegate(typeof(WebMethod), this, method);
                        pageMethod();
                    }
                    else
                        OnDeny(methodName);
                    return;
                }
            }
            EmptyMethod(methodName);
        }

        /// <summary>
        /// Sets the content type, adds optional header, footer, and error handling
        /// </summary>
        protected override void Run()
        {
            var methodName = Read("method", "").ToLower();
            if (string.IsNullOrWhiteSpace(methodName))
                InvokeDefaultPage();
            else if (AllowMethod(methodName))
                InvokeMethod(methodName);
        }
    }
}
