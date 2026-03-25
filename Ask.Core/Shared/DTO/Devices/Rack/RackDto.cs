using Ask.Core.Shared.DTO.Devices.Base;

namespace Ask.DataBase.Provider.Contracts.DTO;

/// <summary>
/// DTO коммутационной стойки.
/// Содержит базовые параметры устройства без логики управления и runtime-состояния.
/// </summary>
public class RackDto : AttachableDeviceDto { }