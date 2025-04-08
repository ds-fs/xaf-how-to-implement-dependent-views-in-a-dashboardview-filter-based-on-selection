using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.Xpo;

namespace DependentDashboard.Module.Controllers
{
    public class DashboardViewItemCurrentObjectFilterController : ViewController<DetailView> //BusinessObjectViewController<Contact>
    {
        public DashboardViewItemCurrentObjectFilterController()
        {

        }
        protected override void OnActivated()
        {
            base.OnActivated();
            UnsubscribeFromViewEvents();
            SubscribeToViewEvents();
            SetCurrentObjectDashboardViewItemCriteria();
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
                View.CurrentObjectChanged += View_CurrentObjectChanged;
            }
        }
        private void UnsubscribeFromViewEvents()
        {
            if (View != null)
            {
                View.CurrentObjectChanged -= View_CurrentObjectChanged;
            }
        }
        void View_CurrentObjectChanged(object sender, EventArgs e) => SetCurrentObjectDashboardViewItemCriteria();
        private void SetCurrentObjectDashboardViewItemCriteria()
        {
            var currentObject = (ObjectSpace.GetObject(View.CurrentObject) as XPBaseObject);
            if (currentObject.IsNull())
                return;
            var dvs = View.Items.Where(x => x.IsOfType<DashboardViewItem>()).Select(x => (DashboardViewItem)x).Where(x => x.Model.Criteria.IsNotEmptyOrWhiteSpace()).ToArray();
            foreach (var item in dvs)
            {
                var lv = item.InnerView as ListView;
                var co = CriteriaOperator.Parse(item.Model.Criteria);
                var props = co.GetCriteriaOperandValues().Select(x => x.Value as string).IgnoreNulls().Where(x => x.ContainsEquivalenceTo("@This.")).ToArray(); //holt die Properties aus dem Filter, die mit @This. anfangen und ersetzt werden müssen

                if (lv != null)
                {
                    try
                    {
                        lv.CollectionSource.Criteria.Clear();
                        var criteria = co.ToString();
                        foreach (var prop in props)
                        {
                            var members = prop.Replace(@"'", "").Replace(@"@This.", "").Split(".");
                            var value = currentObject as object;

                            foreach (var member in members)
                            {
                                if (value.IsNull())
                                    continue;
                                if (value.IsOfTypeOrInherits<XPBaseObject>())
                                    value = (value as XPBaseObject).GetMemberValue(member);
                            }

                            criteria = criteria.Replace(prop, value?.ToString(), StringComparison.CurrentCultureIgnoreCase);
                        }
                        lv.CollectionSource.SetCriteria(nameof(DashboardViewItem), criteria);
                    }
                    catch (Exception ex)
                    {
                        throw new UserFriendlyException(ex);
                    }
                }
            }
        }
    }
}