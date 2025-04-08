using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;

namespace DependentDashboard.Module;

public static class XAFExtensions
{
    public static Task<XPView> LoadAsyncEx(this XPView obj)
    {
        var tcs = new TaskCompletionSource<XPView>();
        obj.LoadAsync((d, ex) =>
        {
            if (ex.IsNull())
                tcs.SetResult(obj);
            else
                tcs.SetException(ex);
        });
        return tcs.Task;
    }

    public static IObjectSpace CreateObjectSpace<T>(this XafApplication obj) where T : XPBaseObject => obj.CreateObjectSpace(typeof(T));

    /// <summary>
    /// <para>Beschreibung:</para>
    /// Erstellt einen <seealso cref="IObjectSpace"/> mit <seealso cref="XafApplication.CreateObjectSpace"/>, <br/>
    /// erstellt einen <seealso cref="DetailView"/>, mit <seealso cref="XafApplication.CreateDetailViewWithAsyncObjectLoad"/>,<br/>
    /// <para>setzt folgende eventArgs.ShowViewParameters:</para>
    /// <code>
    ///     eventArgs.ShowViewParameters.CreateAllControllers = true;<br/>
    ///     eventArgs.ShowViewParameters.TargetWindow = TargetWindow.NewWindow;<br/>
    ///     eventArgs.ShowViewParameters.Context = TemplateContext.View;<br/>
    ///     eventArgs.ShowViewParameters.CreatedView = dv;<br/>
    /// </code>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="objToShow"></param>
    /// <param name="eventArgs"></param>
    /// <returns><seealso cref="DetailView"/></returns>
    public static DetailView CreateDetailView<T>(this XafApplication obj, T objToShow, ActionBaseEventArgs eventArgs) where T : XPBaseObject
    {
        if (objToShow == null)
            return null;
        var os = obj.CreateObjectSpace<T>();
        var dv = obj.CreateDetailViewWithAsyncObjectLoad(os, objToShow, objToShow.GetPropertyValue("Oid"));
        eventArgs.ShowViewParameters.CreateAllControllers = true;
        eventArgs.ShowViewParameters.TargetWindow = TargetWindow.NewWindow;
        eventArgs.ShowViewParameters.Context = TemplateContext.View;
        eventArgs.ShowViewParameters.CreatedView = dv;
        return dv;
    }

    public static void OpenDetailView<T>(this XafApplication obj, T objToShow)
    {
        IObjectSpace os = obj.CreateObjectSpace(typeof(T));
        var itemToOpen = os.GetObject(objToShow);
        var view = obj.CreateDetailView(os, itemToOpen);
        obj.ShowViewStrategy.ShowViewFromCommonView(view, null, null);
    }

    public static DetailView CreateNewObjectAndDetailView<T>(this XafApplication obj, ActionBaseEventArgs eventArgs) where T : XPBaseObject
    {
        var os = obj.CreateObjectSpace<T>();
        var item = os.CreateObject<T>();
        var dv = obj.CreateDetailView(os, item);
        eventArgs.ShowViewParameters.CreatedView = dv;
        return dv;
    }

    public static XPObjectSpace AsXPObjectSpace(this IObjectSpace objectSpace) => objectSpace as XPObjectSpace;
    public static IEnumerable<OperandProperty> GetCriteriaOperandProperties(this CriteriaOperator op) => op switch
    {
        ContainsOperator o => o.Condition.GetCriteriaOperandProperties(),
        AggregateOperand o => o.Condition.GetCriteriaOperandProperties(),
        GroupOperator o => o.Operands.SelectMany(x => x.GetCriteriaOperandProperties()),
        FunctionOperator o => o.Operands.SelectMany(x => x.GetCriteriaOperandProperties()),
        BetweenOperator o => o.BeginExpression.GetCriteriaOperandProperties().Concat(o.EndExpression.GetCriteriaOperandProperties()),
        InOperator o => o.LeftOperand.GetCriteriaOperandProperties(),
        UnaryOperator o => o.Operand.GetCriteriaOperandProperties(),
        BinaryOperator o => o.LeftOperand.GetCriteriaOperandProperties().Concat(o.RightOperand.GetCriteriaOperandProperties()),
        OperandProperty o => [o],
        _ => Enumerable.Empty<OperandProperty>()
    };

    public static IEnumerable<OperandValue> GetCriteriaOperandValues(this CriteriaOperator op) => op switch
    {
        ContainsOperator o => o.Condition.GetCriteriaOperandValues(),
        AggregateOperand o => o.Condition.GetCriteriaOperandValues(),
        GroupOperator o => o.Operands.SelectMany(x => x.GetCriteriaOperandValues()),
        FunctionOperator o => o.Operands.SelectMany(x => x.GetCriteriaOperandValues()),
        BetweenOperator o => o.BeginExpression.GetCriteriaOperandValues().Concat(o.EndExpression.GetCriteriaOperandValues()),
        InOperator o => o.Operands.SelectMany(x => x.GetCriteriaOperandValues()),
        UnaryOperator o => o.Operand.GetCriteriaOperandValues(),
        BinaryOperator o => o.LeftOperand.GetCriteriaOperandValues().Concat(o.RightOperand.GetCriteriaOperandValues()),
        OperandValue o => [o],
        _ => Enumerable.Empty<OperandValue>()
    };

    /// <summary>
    /// Gibt einen Criteria String zurück, z.B. "Oid in (1, 2, 3, 4)".
    /// </summary>
    /// <param name="objects"></param>
    /// <returns></returns>
    public static string GetOidInCriteria(this IEnumerable<XPBaseObject> objects) => $"Oid in ({objects.Select(x => $"'{x.GetMemberValue("Oid")}'").ConcatWith(", ")})";
}
