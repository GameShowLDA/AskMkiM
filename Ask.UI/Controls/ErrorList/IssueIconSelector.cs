using Ask.Core.Services.Errors.Models;
using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Controls.ErrorList
{
  public class IssueIconSelector : DataTemplateSelector
  {
    public DataTemplate ErrorTemplate { get; set; }
    public DataTemplate WarningTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item is IDisplayIssue issue)
      {
        return issue.IsWarning ? WarningTemplate : ErrorTemplate;
      }

      return ErrorTemplate;
    }
  }
}

