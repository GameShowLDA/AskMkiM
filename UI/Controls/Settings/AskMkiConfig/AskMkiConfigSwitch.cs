using Ask.Core.Services.Config.LegacyMki;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Controls.Settings.AskMkiConfig;

/// <summary>
/// Редактор матрицы СК/БК.
/// </summary>
public partial class AskMkiConfigControl
{
  /// <summary>
  /// Создает специальную группу редактирования СК/БК в виде матрицы.
  /// </summary>
  private AskMkiSettingGroup CreateSwitchMatrixGroup(AskMkiGroupDefinition definition, LegacyMkiHardwareProfile profile)
  {
    var group = new AskMkiSettingGroup
    {
      Code = definition.Code,
      Title = definition.Title,
      Description = definition.Description,
      Summary = definition.Summary,
      IsExpanded = false
    };

    var names = StringsFromResource("AskMki.SwitchNames");

    for (var index = 0; index < names.Count; index++)
    {
      var rowIndex = index;
      var rowName = names[rowIndex];

      group.SwitchRows.Add(new AskMkiSwitchRangeRow
      {
        Name = rowName,
        IsFixedPresent = rowIndex == 0,
        FixedDescription = "\\u0412\\u0441\\u0435\\u0433\\u0434\\u0430",
        IsPresent = rowIndex == 0 || IsTruthy(GetValueByPath(profile, $"HardwareConfig.SkIs[{rowIndex}]")),
        FirstBk = Convert.ToString(GetValueByPath(profile, $"HardwareConfig.SkBkBeg[{rowIndex}]"), CultureInfo.InvariantCulture) ?? string.Empty,
        LastBk = Convert.ToString(GetValueByPath(profile, $"HardwareConfig.SkBkEnd[{rowIndex}]"), CultureInfo.InvariantCulture) ?? string.Empty,
        ApplyToProfile = (targetProfile, row) =>
        {
          if (rowIndex == 0)
          {
            SetValueByPath(
              targetProfile,
              $"HardwareConfig.SkIs[{rowIndex}]",
              ConvertToggleToType(true, GetValueTypeByPath(targetProfile, $"HardwareConfig.SkIs[{rowIndex}]")));
          }
          else
          {
            SetValueByPath(
              targetProfile,
              $"HardwareConfig.SkIs[{rowIndex}]",
              ConvertToggleToType(row.IsPresent, GetValueTypeByPath(targetProfile, $"HardwareConfig.SkIs[{rowIndex}]")));
          }

          SetValueByPath(
            targetProfile,
            $"HardwareConfig.SkBkBeg[{rowIndex}]",
            ConvertTextToType(row.FirstBk, GetValueTypeByPath(targetProfile, $"HardwareConfig.SkBkBeg[{rowIndex}]")));

          SetValueByPath(
            targetProfile,
            $"HardwareConfig.SkBkEnd[{rowIndex}]",
            ConvertTextToType(row.LastBk, GetValueTypeByPath(targetProfile, $"HardwareConfig.SkBkEnd[{rowIndex}]")));
        }
      });
    }

    return group;
  }

  /// <summary>
  /// Ограничивает ввод номера БК двумя цифрами.
  /// </summary>
  private void SmallNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
  {
    if (sender is not TextBox textBox)
    {
      return;
    }

    var newText = GetProposedText(textBox, e.Text);

    e.Handled = newText.Length > 2 || newText.Any(ch => !char.IsDigit(ch));
  }

  /// <summary>
  /// Проверяет вставляемое значение номера БК.
  /// </summary>
  private void SmallNumberTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
  {
    if (!e.DataObject.GetDataPresent(DataFormats.Text))
    {
      e.CancelCommand();
      return;
    }

    if (sender is not TextBox textBox)
    {
      e.CancelCommand();
      return;
    }

    var pastedText = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
    var newText = GetProposedText(textBox, pastedText);

    if (newText.Length > 2 || newText.Any(ch => !char.IsDigit(ch)))
    {
      e.CancelCommand();
    }
  }

  /// <summary>
  /// Возвращает текст поля после предполагаемого ввода или вставки.
  /// </summary>
  private static string GetProposedText(TextBox textBox, string newTextPart)
  {
    var currentText = textBox.Text ?? string.Empty;

    var selectionStart = textBox.SelectionStart;
    var selectionLength = textBox.SelectionLength;

    if (selectionLength > 0)
    {
      currentText = currentText.Remove(selectionStart, selectionLength);
    }

    return currentText.Insert(selectionStart, newTextPart);
  }
}
