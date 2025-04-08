using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;




namespace DependentDashboard.Module.Controllers;

// For more typical usage scenarios, be sure to check out https://documentation.devexpress.com/eXpressAppFramework/clsDevExpressExpressAppViewControllertopic.aspx.
// Erklärung unter: https://blog.delegate.at/2018/03/10/xaf-best-practices-2017-03.html
public abstract class BusinessObjectDetailViewController<TObjectType> : BusinessObjectViewController<DetailView, TObjectType>
    where TObjectType : class
{
}

public abstract class BusinessObjectListViewController<TObjectType> : BusinessObjectViewController<ListView, TObjectType>
    where TObjectType : class
{
}

public abstract class BusinessObjectViewController<TObjectType> : BusinessObjectViewController<ObjectView, TObjectType>
     where TObjectType : class
{
}

public abstract class BusinessObjectViewController<TView, TObjectType> : ViewController<TView>
    where TObjectType : class
    where TView : ObjectView
{
    private readonly System.Threading.SynchronizationContext synchronizationContext;

    public event EventHandler<CurrentObjectChangingEventArgs<TObjectType>> CurrentObjectChanging;
    public event EventHandler<CurrentObjectChangedEventArgs<TObjectType>> CurrentObjectChanged;

    protected BusinessObjectViewController()
    {
        TargetObjectType = typeof(TObjectType);
        synchronizationContext = AsyncOperationManager.SynchronizationContext;
    }


    protected override void OnActivated()
    {
        base.OnActivated();

        UnsubscribeFromViewEvents();
        SubscribeToViewEvents();
    }

    protected override void OnDeactivated()
    {
        UnsubscribeFromViewEvents();

        base.OnDeactivated();
    }

    private void SubscribeToViewEvents()
    {
        if (View != null)
        {
            View.QueryCanChangeCurrentObject += View_QueryCanChangeCurrentObject;
            View.CurrentObjectChanged += View_CurrentObjectChanged;
        }
    }

    private void UnsubscribeFromViewEvents()
    {
        if (View != null)
        {
            View.QueryCanChangeCurrentObject -= View_QueryCanChangeCurrentObject;
            View.CurrentObjectChanged -= View_CurrentObjectChanged;
        }
    }

    void View_QueryCanChangeCurrentObject(object sender, CancelEventArgs e)
    {
        var args = new CurrentObjectChangingEventArgs<TObjectType>(e.Cancel, CurrentObject);
        OnCurrentObjectChanging((TView)sender, args);
        e.Cancel = args.Cancel;
    }

    protected virtual void OnCurrentObjectChanging(TView view, CurrentObjectChangingEventArgs<TObjectType> e)
        => CurrentObjectChanging?.Invoke(this, e);

    void View_CurrentObjectChanged(object sender, EventArgs e)
        => OnCurrentObjectChanged((TView)sender, new CurrentObjectChangedEventArgs<TObjectType>(CurrentObject));

    protected virtual void OnCurrentObjectChanged(TView view, CurrentObjectChangedEventArgs<TObjectType> args)
        => CurrentObjectChanged?.Invoke(this, args);
    private TObjectType GetCurrentObject() => View?.ObjectSpace.GetObject(View.CurrentObject) as TObjectType;

    public TObjectType CurrentObject => View?.CurrentObject switch
    {
        ObjectRecord => GetCurrentObject(),
        XpoDataViewRecord => GetCurrentObject(),
        TObjectType => View?.CurrentObject as TObjectType,
        _ => null
    };

    public IEnumerable<TObjectType> SelectedObjects
    {
        get
        {
            if (!(View is null) || View.SelectedObjects.Count > 0)
            {
                Type type = View?.CurrentObject?.GetType();
                if (type == typeof(TObjectType))
                {
                    foreach (var item in View?.SelectedObjects?.OfType<TObjectType>())
                        yield return item;
                }
                else if (type == typeof(ObjectRecord) || type == typeof(XpoDataViewRecord) || type == typeof(XafInstantFeedbackRecord))
                {

                    foreach (var item in View?.SelectedObjects)
                        yield return View.ObjectSpace.GetObject(item) as TObjectType;
                }
            }
            foreach (var item in Enumerable.Empty<TObjectType>())
                yield return item;
        }
    }

    public void RunInUIThread(Action action)
        => synchronizationContext.Post((o) => action(), this);


    public void RaiseCurrentObjectChanged(object sender)
    {
        OnCurrentObjectChanged((TView)sender, new CurrentObjectChangedEventArgs<TObjectType>(CurrentObject));
    }

}

public class CurrentObjectChangedEventArgs<TObjectType> : EventArgs
    where TObjectType : class
{
    public readonly TObjectType CurrentObject;

    public CurrentObjectChangedEventArgs(TObjectType obj)
        => CurrentObject = obj;
}

public class CurrentObjectChangingEventArgs<TObjectType> : EventArgs
    where TObjectType : class
{
    public readonly TObjectType CurrentObject;

    public bool Cancel { get; set; }

    public CurrentObjectChangingEventArgs(TObjectType obj) : this(false, obj)
    {
    }

    public CurrentObjectChangingEventArgs(bool cancel, TObjectType obj)
    {
        Cancel = cancel;
        CurrentObject = obj;
    }
}
