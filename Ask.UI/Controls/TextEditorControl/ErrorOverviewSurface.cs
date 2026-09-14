using System.Windows;
using System.Windows.Media;

namespace Ask.UI.Controls.TextEditorControl
{
  /// <summary>
  /// Отрисовывает маркеры и область видимости внутри XAML-компонента обзора.
  /// </summary>
  public sealed class ErrorOverviewSurface : FrameworkElement
  {
    internal ErrorOverviewBar? Owner { get; set; }

    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
    {
      if (Owner?.IsMarkerHitTestOnly == true)
        return Owner.IsMarkerAt(hitTestParameters.HitPoint)
          ? new PointHitTestResult(this, hitTestParameters.HitPoint)
          : null;

      return base.HitTestCore(hitTestParameters);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
      base.OnRender(drawingContext);
      Owner?.RenderOverview(drawingContext);
    }
  }
}
