using Ask.Core.Services.Config.LegacyMki;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace UI.Controls.Settings.AskMkiConfig;

/// <summary>
/// Построение групп редактора конфигурации.
/// </summary>
public partial class AskMkiConfigControl
{
  /// <summary>
  /// Строит коллекцию групп редактора по XAML-описанию полей.
  /// </summary>
  private ObservableCollection<AskMkiSettingGroup> BuildEditorGroups(LegacyMkiHardwareProfile profile)
  {
    _currentProfile = profile;

    var groups = new ObservableCollection<AskMkiSettingGroup>();

    foreach (var definition in GroupDefinitions("AskMki.Groups"))
    {
      if (string.Equals(definition.Code, "A", StringComparison.OrdinalIgnoreCase))
      {
        groups.Add(CreateSwitchMatrixGroup(definition, profile));
      }
      else
      {
        groups.Add(CreateFlatGroupFromDefinition(definition));
      }
    }

    return groups;
  }

  /// <summary>
  /// Возвращает определения групп из ресурсов XAML.
  /// </summary>
  private IReadOnlyList<AskMkiGroupDefinition> GroupDefinitions(string key)
  {
    return ((IEnumerable)FindResource(key)).Cast<AskMkiGroupDefinition>().ToArray();
  }

  /// <summary>
  /// Возвращает определения полей из ресурсов XAML.
  /// </summary>
  private IReadOnlyList<AskMkiFieldDefinition> FieldDefinitions(string key)
  {
    return ((IEnumerable)FindResource(key)).Cast<AskMkiFieldDefinition>().ToArray();
  }

  /// <summary>
  /// Возвращает варианты выбора из ресурсов XAML.
  /// </summary>
  private IReadOnlyList<AskMkiSettingOption> OptionsFromResource(string key)
  {
    return ((IEnumerable)FindResource(key)).Cast<AskMkiSettingOption>().ToArray();
  }

  /// <summary>
  /// Возвращает строковый список из ресурсов XAML.
  /// </summary>
  private IReadOnlyList<string> StringsFromResource(string key)
  {
    return ((IEnumerable)FindResource(key)).Cast<string>().ToArray();
  }

  /// <summary>
  /// Создает обычную плоскую группу полей редактора.
  /// </summary>
  private AskMkiSettingGroup CreateFlatGroupFromDefinition(AskMkiGroupDefinition definition)
  {
    var group = new AskMkiSettingGroup
    {
      Code = definition.Code,
      Title = definition.Title,
      Description = definition.Description,
      Summary = definition.Summary,
      IsExpanded = false
    };

    AddFieldsRecursive(group, definition);

    return group;
  }

  /// <summary>
  /// Рекурсивно добавляет поля дочерних определений в целевую группу.
  /// </summary>
  private void AddFieldsRecursive(AskMkiSettingGroup targetGroup, AskMkiGroupDefinition definition)
  {
    if (!string.IsNullOrWhiteSpace(definition.FieldsKey))
    {
      AddDefinitionItems(targetGroup, definition.FieldsKey);
    }

    foreach (var child in definition.Children)
    {
      AddFieldsRecursive(targetGroup, child);
    }
  }

  /// <summary>
  /// Добавляет в группу поля из указанного XAML-ресурса.
  /// </summary>
  private void AddDefinitionItems(AskMkiSettingGroup group, string resourceKey)
  {
    foreach (var definition in FieldDefinitions(resourceKey))
    {
      foreach (var expandedDefinition in ExpandDefinition(definition))
      {
        if (ShouldSkipEditorDefinition(expandedDefinition))
        {
          continue;
        }

        group.Items.Add(CreateItemFromDefinition(expandedDefinition));
      }
    }
  }

  /// <summary>
  /// Проверяет, нужно ли пропустить поле при первичном построении редактора.
  /// </summary>
  private bool ShouldSkipEditorDefinition(AskMkiFieldDefinition definition)
  {
    return false;
  }

  /// <summary>
  /// Разворачивает повторяющееся определение поля в отдельные элементы.
  /// </summary>
  private IEnumerable<AskMkiFieldDefinition> ExpandDefinition(AskMkiFieldDefinition definition)
  {
    if (definition.Count <= 0)
    {
      yield return definition;
      yield break;
    }

    IReadOnlyList<string>? names = null;
    if (!string.IsNullOrWhiteSpace(definition.NamesKey))
    {
      names = StringsFromResource(definition.NamesKey);
    }

    for (var offset = 0; offset < definition.Count; offset++)
    {
      var index = definition.StartIndex + offset;

      var labelArgument = names != null && index < names.Count
        ? names[index]
        : (index + 1).ToString(CultureInfo.CurrentCulture);

      yield return new AskMkiFieldDefinition
      {
        Label = string.Format(CultureInfo.CurrentCulture, definition.LabelFormat, labelArgument),
        Description = definition.Description,
        EditorKind = definition.EditorKind,
        Path = string.Format(CultureInfo.InvariantCulture, definition.PathFormat, index),
        OptionsKey = definition.OptionsKey
      };
    }
  }

  /// <summary>
  /// Создает элемент редактора по определению поля.
  /// </summary>
  private AskMkiSettingItem CreateItemFromDefinition(AskMkiFieldDefinition definition)
  {
    if (definition.EditorKind == AskMkiSettingEditorKind.Info)
    {
      return new AskMkiSettingItem
      {
        Label = definition.Label,
        Description = definition.Description,
        Path = definition.Path,
        EditorKind = AskMkiSettingEditorKind.Info,
        ApplyToProfile = (_, _) => { }
      };
    }

    var value = GetValueByPath(CurrentProfile(), definition.Path);

    return definition.EditorKind switch
    {
      AskMkiSettingEditorKind.Text => new AskMkiSettingItem
      {
        Label = definition.Label,
        Description = definition.Description,
        Path = definition.Path,
        EditorKind = AskMkiSettingEditorKind.Text,
        TextValue = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        ApplyToProfile = (profile, item) =>
        {
          var targetType = GetValueTypeByPath(profile, definition.Path);
          SetValueByPath(profile, definition.Path, ConvertTextToType(item.TextValue, targetType));
        }
      },

      AskMkiSettingEditorKind.Toggle => new AskMkiSettingItem
      {
        Label = definition.Label,
        Description = definition.Description,
        Path = definition.Path,
        EditorKind = AskMkiSettingEditorKind.Toggle,
        ToggleValue = IsTruthy(value),
        ApplyToProfile = (profile, item) =>
        {
          var targetType = GetValueTypeByPath(profile, definition.Path);
          SetValueByPath(profile, definition.Path, ConvertToggleToType(item.ToggleValue, targetType));
        }
      },

      AskMkiSettingEditorKind.Choice => CreateChoiceItemFromDefinition(definition, value),

      _ => throw new InvalidOperationException(UiFormat("AskMki.Error.UnknownEditorType", definition.EditorKind))
    };
  }

  /// <summary>
  /// Создает элемент редактора с выпадающим списком.
  /// </summary>
  private AskMkiSettingItem CreateChoiceItemFromDefinition(AskMkiFieldDefinition definition, object? value)
  {
    if (string.IsNullOrWhiteSpace(definition.OptionsKey))
    {
      throw new InvalidOperationException(UiFormat("AskMki.Error.OptionsKeyMissing", definition.Label));
    }

    var options = OptionsFromResource(definition.OptionsKey);

    var item = new AskMkiSettingItem
    {
      Label = definition.Label,
      Description = definition.Description,
      Path = definition.Path,
      EditorKind = AskMkiSettingEditorKind.Choice,
      Options = new ObservableCollection<AskMkiSettingOption>(options),
      ApplyToProfile = (profile, editorItem) =>
      {
        if (editorItem.SelectedOption == null)
        {
          throw new InvalidOperationException(UiFormat("AskMki.Error.OptionNotSelected", editorItem.Label));
        }

        var targetType = GetValueTypeByPath(profile, definition.Path);
        SetValueByPath(profile, definition.Path, ConvertTextToType(editorItem.SelectedOption.Value.ToString(CultureInfo.InvariantCulture), targetType));
      }
    };

    item.SelectedOption = item.Options.FirstOrDefault(option =>
      string.Equals(
        option.Value.ToString(CultureInfo.InvariantCulture),
        Convert.ToString(value, CultureInfo.InvariantCulture),
        StringComparison.OrdinalIgnoreCase));

    return item;
  }
}
