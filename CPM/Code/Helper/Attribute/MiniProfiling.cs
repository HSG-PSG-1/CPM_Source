using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using StackExchange.Profiling;

namespace CPM.Helper
{
    public class ProfilingActionFilter : ActionFilterAttribute
    {
        const string stackKey = "ProfilingActionFilterStack";

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var mp = MiniProfiler.Current;
            if (mp != null)
            {
                var stack = HttpContext.Current.Items[stackKey] as Stack<IDisposable>;
                if (stack == null)
                {
                    stack = new Stack<IDisposable>();
                    HttpContext.Current.Items[stackKey] = stack;
                }

                var prof = MiniProfiler.Current.Step("Controller: " + filterContext.Controller.ToString() + "." + filterContext.ActionDescriptor.ActionName);
                stack.Push(prof);

            }
            base.OnActionExecuting(filterContext);
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);
            var stack = HttpContext.Current.Items[stackKey] as Stack<IDisposable>;
            if (stack != null && stack.Count > 0)
            {
                stack.Pop().Dispose();
            }
        }
    }

    public class ProfilingViewEngine : IViewEngine
    {
        class WrappedView : IView
        {
            IView wrapped;
            string name;
            bool isPartial;

            public WrappedView(IView wrapped, string name, bool isPartial)
            {
                this.wrapped = wrapped;
                this.name = name;
                this.isPartial = isPartial;
            }

            public void Render(ViewContext viewContext, System.IO.TextWriter writer)
            {
                using (MiniProfiler.Current.Step("Render " + (isPartial ? "partial" : "") + ": " + name))
                {
                    wrapped.Render(viewContext, writer);
                }
            }
        }

        IViewEngine wrapped;

        public ProfilingViewEngine(IViewEngine wrapped)
        {
            this.wrapped = wrapped;
        }

        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            var found = wrapped.FindPartialView(controllerContext, partialViewName, useCache);
            if (found != null && found.View != null)
            {
                found = new ViewEngineResult(new WrappedView(found.View, partialViewName, isPartial: true), this);
            }
            return found;
        }

        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            var found = wrapped.FindView(controllerContext, viewName, masterName, useCache);
            if (found != null && found.View != null)
            {
                found = new ViewEngineResult(new WrappedView(found.View, viewName, isPartial: false), this);
            }
            return found;
        }

        public void ReleaseView(ControllerContext controllerContext, IView view)
        {
            wrapped.ReleaseView(controllerContext, view);
        }
    }
}