using Microsoft.AspNetCore.Components.Web;
using TabBlazor.Services;

namespace TabBlazor.Components.Tables
{
    public class TableRowBase<TableItem> : TableRowComponentBase<TableItem>
    {
        protected ElementReference[] tableCells;
        [Inject] private TablerService tabService { get; set; }

        [Parameter] public ITableRow<TableItem> Table { get; set; }
        [Parameter] public TableItem Item { get; set; }
        [Parameter] public ITableRowActions<TableItem> Actions { get; set; }

        protected bool ShowRowAction => Table.RowActionTemplate != null || Table.AllowDelete || Table.AllowEdit;

        protected override void OnInitialized() => tableCells = new ElementReference[Table.VisibleColumns.Count + 2];

        protected int GetTabIndex() => Table.KeyboardNavigation ? 0 : -1;

        protected bool CanDelete()
        {
            if (!Table.AllowDelete)
            {
                return false;
            }

            if (Table.AllowDeleteExpression == null)
            {
                return true;
            }

            return Table.AllowDeleteExpression(Item);
        }

        protected bool CanEdit()
        {
            if (!Table.AllowEdit)
            {
                return false;
            }

            if (Table.AllowEditExpression == null)
            {
                return true;
            }

            return Table.AllowEditExpression(Item);
        }


        public string GetRowCssClass(TableItem item) =>
            new ClassBuilder()
                .Add("data-row")
                .AddIf("table-active", IsSelected(item) && (Table.OnItemSelected.HasDelegate || Table.SelectedItemsChanged.HasDelegate))
                .ToString();

        protected async Task OnKeyDown(KeyboardEventArgs e, ElementReference tableCell)
        {
            if (e.Key == "ArrowUp" || e.Key == "ArrowDown")
            {
                await tabService.NavigateTable(tableCell, e.Key);
            }
        }

        public async Task RowClick() => await Table.RowClicked(Item);

        public bool IsSelected(TableItem item)
        {
            if (Table.SelectedItems == null)
            {
                return false;
            }

            return Table.SelectedItems.Contains(item);
        }

        protected async Task Delete()
        {
            if (Table.IsAddInProgress)
            {
                return; // Delete gets triggered by pressing 'Enter' while adding a new item
            }

            await Table.OnDeleteItem(Item);
        }

        protected void Edit() => Table.EditItem(Item);
    }
}
