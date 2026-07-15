using Ask.Core.Services.Config.LegacyMki;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
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
    if (string.Equals(key, "AskMki.Options.RuntimeMultimeters", StringComparison.Ordinal))
    {
      return RuntimeMultimeterOptions();
    }

    return ((IEnumerable)FindResource(key))
      .Cast<AskMkiSettingOption>()
      .Select(CloneOption)
      .ToArray();
  }

  private static AskMkiSettingOption CloneOption(AskMkiSettingOption option)
  {
    return new AskMkiSettingOption
    {
      Value = option.Value,
      Label = option.Label
    };
  }

  /// <summary>
  /// Возвращает список runtime-мультиметров, доступных в общей системе устройств.
  /// </summary>
  private static IReadOnlyList<AskMkiSettingOption> RuntimeMultimeterOptions()
  {
    return RuntimeMultimeterTypes
      .Select(CreateMultimeterOption)
      .Where(option => option != null)
      .Cast<AskMkiSettingOption>()
      .OrderBy(option => option.Label, StringComparer.CurrentCultureIgnoreCase)
      .ToArray();
  }

  /// <summary>
  /// Создает вариант выбора мультиметра по runtime-классу устройства.
  /// </summary>
  private static AskMkiSettingOption? CreateMultimeterOption(Type type)
  {
    if (Activator.CreateInstance(type) is not IMultimeter meter)
    {
      return null;
    }

    return new AskMkiSettingOption
    {
      Value = type.FullName ?? type.Name,
      Label = meter.Name
    };
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

    if (definition.EditorKind == AskMkiSettingEditorKind.Text
        && definition.Path == "HardwareAux.UsbAddrVm")
    {
      return CreateUsbDeviceItem(definition, value);
    }

    if (definition.EditorKind == AskMkiSettingEditorKind.Text
        && definition.Path == "HardwareAux.VoltmeterIpAddress")
    {
      return CreateIpAddressItem(definition, value);
    }

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
        var selectedOption = editorItem.SelectedOption;
        if (definition.Path == "HardwareAux.VoltmeterConnectionType" && string.IsNullOrWhiteSpace(selectedOption?.Value))
        {
          selectedOption = editorItem.Options.FirstOrDefault(option => !string.IsNullOrWhiteSpace(option.Value));
        }

        if (selectedOption == null)
        {
          throw new InvalidOperationException(UiFormat("AskMki.Error.OptionNotSelected", editorItem.Label));
        }

        var targetType = GetValueTypeByPath(profile, definition.Path);
        SetValueByPath(profile, definition.Path, ConvertTextToType(selectedOption.Value, targetType));

        if (definition.Path is "HardwareAux.VoltmeterDeviceClass" or "HardwareAux.VoltmeterConnectionType")
        {
          profile.HardwareConfig.DvV7 = 7;
        }
      }
    };

    item.SelectedOption = item.Options.FirstOrDefault(option =>
      string.Equals(
        option.Value,
        Convert.ToString(value, CultureInfo.InvariantCulture),
        StringComparison.OrdinalIgnoreCase));

    item.SelectedOption ??= item.Options.FirstOrDefault();

    return item;
  }

  /// <summary>
  /// Создает карточку USB-устройства для подключения цифрового вольтметра.
  /// </summary>
  private AskMkiSettingItem CreateUsbDeviceItem(AskMkiFieldDefinition definition, object? value)
  {
    var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    var item = new AskMkiSettingItem
    {
      Label = definition.Label,
      Description = definition.Description,
      Path = definition.Path,
      EditorKind = AskMkiSettingEditorKind.UsbDevice,
      TextValue = text,
      UsbStatus = string.IsNullOrWhiteSpace(text) ? "USB не найден" : $"USB устройство: {text}",
      UsbVid = "N/A",
      UsbPid = "N/A",
      ApplyToProfile = (profile, editorItem) =>
      {
        var targetType = GetValueTypeByPath(profile, definition.Path);
        SetValueByPath(profile, definition.Path, ConvertTextToType(editorItem.TextValue, targetType));
      }
    };

    FillUsbInfoFromAddress(item, text);
    return item;
  }

  /// <summary>
  /// Создает карточку IP-адреса для подключения цифрового вольтметра.
  /// </summary>
  private AskMkiSettingItem CreateIpAddressItem(AskMkiFieldDefinition definition, object? value)
  {
    var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    var item = new AskMkiSettingItem
    {
      Label = definition.Label,
      Description = definition.Description,
      Path = definition.Path,
      EditorKind = AskMkiSettingEditorKind.IpAddress,
      ApplyToProfile = (profile, editorItem) =>
      {
        var targetType = GetValueTypeByPath(profile, definition.Path);
        SetValueByPath(profile, definition.Path, ConvertTextToType(editorItem.TextValue, targetType));
      }
    };

    item.SetIpTextValue(string.IsNullOrWhiteSpace(text) ? "192.168.1." : text);
    return item;
  }
}
