namespace Ask.Core.Services.Extensions
{
  public static class EnumConverter
  {
    /// <summary>
    /// Пытается преобразовать строку в enum заданного типа.
    /// </summary>
    public static bool TryParseEnum<TEnum>(
      string value,
      out TEnum result,
      bool ignoreCase = true)
      where TEnum : struct, Enum
    {
      result = default;

      if (string.IsNullOrWhiteSpace(value))
        return false;

      return Enum.TryParse(value.Trim(), ignoreCase, out result);
    }
  }
}
