using System;
using System.Windows;

namespace UI.Windows.WpfDocking.Windows.Docking
{
    partial class DockItem
    {
        private struct UpdateAutoHideSizeData
        {
            public DockItem DockItem;
            public SplitterDistance Value;

            public UpdateAutoHideSizeData(DockItem dockItem, SplitterDistance value)
            {
                DockItem = dockItem;
                Value = value;
            }
        }
    }
}
