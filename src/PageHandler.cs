namespace Codebot.Web;

using System;
using System.Linq;

/// <summary>
/// Page handler is a typically the base class your should derive from
/// to handle requests. You may adorn you class with [DefaultPage] or
/// its actions with [Action]
/// </summary>
public class PageHandler : BasicHandler
{
    /// <summary>
    /// Allow the name of the action parameter to be redefined from its default value
    /// </summary>
    protected static string Action { get; set; }

    /// <summary>
    /// Allow the user to redefine the action identifier
    /// </summary>
    static PageHandler() => Action = "action";

    /// <summary>
    /// The signature of a web action
    /// </summary>
    public delegate void WebAction();

    /// <summary>
    /// Invoked when no default page is found
    /// </summary>
    protected virtual void EmptyPage() { }

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
    /// If this is not a action page request then check for a default page
    /// </summary>
    private void InvokeDefaultPage()
    {
        if ((!IsAuthenticated && InvokePageType<LoginPageAttribute>()) || InvokePageType<DefaultPageAttribute>())
            return;
        EmptyPage();
    }

    /// <summary>
    /// Invoked when no action is found
    /// </summary>
    protected virtual void EmptyAction(string actionName) => InvokeDefaultPage();

    /// <summary>
    /// Provide a chance for decedents to supersede ActionPage rights
    /// </summary>
    protected virtual bool AllowAction(string actionName) => true;

    /// <summary>
    /// The action was denied and you can do something about it here
    /// </summary>
    protected virtual void OnDeny(string actionName) { }

    /// <summary>
    /// The action was allowed and you can do something about it here
    /// </summary>
    protected virtual void OnAllow(string actionName) { }

    /// <summary>
    /// Check the ActionPage for deny rights
    /// </summary>
    private bool Deny(ActionAttribute a)
    {
        var deny = a.Deny;
        if (string.IsNullOrEmpty(deny))
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
    /// Check the ActionPage for allow rights
    /// </summary>
    private bool Allow(ActionAttribute a)
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
    /// Attempt to find a matching ActionPage attribute, and verify user rights
    /// </summary>
    private void InvokeAction(string actionName)
    {
        foreach (var action in GetType().GetMethods())
        {
            var attribute = Array.Find(
                action.GetCustomAttributes<ActionAttribute>(true),
                a => a.ActionName.ToLower() == actionName);
            if (attribute != null)
            {
                if (Deny(attribute))
                    OnDeny(actionName);
                else if (Allow(attribute))
                {
                    OnAllow(actionName);
                    ContentType = attribute.ContentType;
                    var pageAction = (WebAction)Delegate.CreateDelegate(typeof(WebAction), this, action);
                    pageAction();
                }
                else
                    OnDeny(actionName);
                return;
            }
        }
        EmptyAction(actionName);
    }

    /// <summary>
    /// Look for a action or invoked the default page
    /// </summary>
    protected override void Run()
    {
        var actionName = Read(Action, "").ToLower();
        if (string.IsNullOrWhiteSpace(actionName))
            InvokeDefaultPage();
        else if (AllowAction(actionName))
            InvokeAction(actionName);
    }
}
