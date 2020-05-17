using System;
using System.Linq;

namespace Codebot.Web
{
    /// <summary>
    /// Page handler is a typically the base class your should derive from 
    /// to handle requests. You may adorn you class with [DefaultPage] or
    /// its methods with [MethodPage]
    /// </summary>
    public class PageHandler : BasicHandler
    {
        /// <summary>
        /// Allow the name of the method parameter to be redefined from its default value
        /// </summary>
        protected static string Method { get; set; }

        static PageHandler()
        {
            Method = "method";
        }

        /// <summary>
        /// The signature of a web method
        /// </summary>
        public delegate void WebMethod();

        /// <summary>
        /// Invoked when no default page is found
        /// </summary>
        protected virtual void EmptyPage()
        {
        }

        /// <summary>
        /// Check for a PageType derived attribute including DefaultPage
        /// </summary>
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

        /// <summary>
        /// If this is not a method page request then check for a default page
        /// </summary>
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
        /// Provide a chance for desedent to superceed MethodPage rights
        /// </summary>
        protected virtual bool AllowMethod(string methodName)
        {
            return true;
        }

        /// <summary>
        /// The method was denied and you can do something about it here
        /// </summary>
        protected virtual void OnDeny(string methodName)
        {
        }

        /// <summary>
        /// The method was allowed and you can do something about it here
        /// </summary>
        protected virtual void OnAllow(string methodName)
        {
        }

        /// <summary>
        /// Check the MethodPage for deny rights 
        /// </summary>
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

        /// <summary>
        /// Check the MethodPage for allow rights 
        /// </summary>
        private bool Allow(MethodPageAttribute a)
        {
            var allow = a.Allow;
            if (string.IsNullOrEmpty(allow))
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

        /// <summary>
        /// Attempt to find a matching MethodPage attribute, and verify user rights
        /// </summary>
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
        /// Look for a method or invoked the default page
        /// </summary>
        protected override void Run()
        {
            var methodName = Read(Method, "").ToLower();
            if (string.IsNullOrWhiteSpace(methodName))
                InvokeDefaultPage();
            else if (AllowMethod(methodName))
                InvokeMethod(methodName);
        }
    }
}
