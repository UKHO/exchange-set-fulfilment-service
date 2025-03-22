namespace TabBlazor.Dashboards
{
    public partial class RangeFilter<TItem> : DashboardComponent<TItem> where TItem : class
    {
        private decimal allMax;

        private decimal allMin;

        private DataFilter<TItem> filter;
        private decimal max;

        private decimal min;
        [Parameter] public Expression<Func<TItem, decimal>> Expression { get; set; }
        [Parameter] public string Name { get; set; }

        [Parameter] public RenderFragment<DataFacet<TItem>> Facet { get; set; }

        protected override void OnInitialized()
        {
            allMin = Dashboard.AllItems.Min(Expression);
            allMax = Dashboard.AllItems.Max(Expression);

            min = allMin;
            max = allMax;
        }

        private void RemoveFilter()
        {
            min = allMin;
            max = allMax;
            Dashboard.RemoveFilter(filter);
            filter = null;
        }

        private void MinUpdated(ChangeEventArgs e)
        {
            if (decimal.TryParse(e.Value.ToString(), out var inputValue))
            {
                if (inputValue < allMin)
                {
                    inputValue = allMin;
                }

                min = inputValue;
                FilterData();
            }
        }

        private void MaxUpdated(ChangeEventArgs e)
        {
            if (decimal.TryParse(e.Value.ToString(), out var inputValue))
            {
                if (inputValue > allMax)
                {
                    inputValue = allMax;
                }

                max = inputValue;
                FilterData();
            }
        }


        private void FilterData()
        {
            var predicate = FacetsHelper.CreateRangePredicate(Expression, min, max);

            if (filter == null)
            {
                filter = new DataFilter<TItem> { Expression = predicate, Name = Name };

                Dashboard.AddFilter(filter);
            }
            else
            {
                filter.Expression = predicate;
                Dashboard.RunFilter();
            }
        }
    }
}
