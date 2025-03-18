namespace NewCore.Base.Function.DBC
{
  /// <summary>
  /// Тип проверки цепи самоконтроля.
  /// </summary>
  public enum SelfTestType
  {
    BlockingRelay = 1,     // Блокировочное реле
    Multimeter = 2,        // Мультиметр
    ADC = 3,               // АЦП
    ADCReversed = 4,       // АЦП с переполюсовкой
    PINT = 5,              // ПИНТ
    Shunt = 6              // Шунт
  }
}
