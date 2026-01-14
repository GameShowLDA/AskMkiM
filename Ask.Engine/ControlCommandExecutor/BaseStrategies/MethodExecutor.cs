using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies
{
  internal static class MethodExecutor
  {
    /// <summary>
    /// Количество разрядов в двоичном представлении номера точки.
    /// </summary>
    private static int HighestBitCount { get; set; }

    /// <summary>
    /// Выполняет последовательную проверку точек групповым методом.
    /// </summary>
    /// <param name="points">Список точек для проверки.</param>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <returns>Задача, представляющая выполнение проверки.</returns>
    static public async Task<List<ShowMessageModel>> CheckSequenceAsync(MethodExecutionContext methodExecutionContext)
    {
      List<ShowMessageModel> showMessageModels = new List<ShowMessageModel>();

      var pointsList = methodExecutionContext.SchemeModel.GetPointsDisconnected();
      if (pointsList.Count == 0)
      {
        return showMessageModels;
      }

      await methodExecutionContext.MessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildCheckBlockHeader(ControlCheckAlgorithm.Group));

      List<ChainModel> chains = new List<ChainModel>();
      foreach (var point in pointsList)
      {
        chains.Add(new ChainModel(point));
      }

      HighestBitCount = GetHighestPointBinaryDigits(chains);
      var binaryPoints = ConvertToReversedBinaryRange(chains, HighestBitCount);


      for (int step = 0; step < HighestBitCount; step++)
      {
        char[] bits = new char[HighestBitCount];
        for (int i = 0; i < bits.Length; i++)
          bits[i] = '0';

        bits[HighestBitCount - step - 1] = '1';

        string stepStr = new string(bits);

        await methodExecutionContext.MessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildDischargeCheckBlock(ConvertIntToString(step + 1)), IsBlockStart: true);

        await ConnectPointsToBusAsync(binaryPoints, methodExecutionContext.SchemeModel, step, methodExecutionContext.MessageService);
        var result = await methodExecutionContext.PerformMeasurementAsync(methodExecutionContext.Value, methodExecutionContext.MessageService, methodExecutionContext.MessageService.GetCancellationToken());
        if (!result.Result)
        {
          await DisconnectPointsToBusAsync(binaryPoints, methodExecutionContext.SchemeModel, step, methodExecutionContext.MessageService);

          await methodExecutionContext.MessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildDischargeCheckError(stepStr), IsBlockStart: true);
          showMessageModels.Add(new ShowMessageModel($"Разряд {stepStr}({methodExecutionContext.LowerLimit}{(methodExecutionContext.HigherLimit != -1 ? $"-{methodExecutionContext.HigherLimit}" : "<")}{methodExecutionContext.Unit})", message: $"{methodExecutionContext.UnitMnemonic}изм = {result.Value} {methodExecutionContext.Unit}. Переход к методу полного узла", type: ShowMessageModel.MessageType.Error));

          NodeFullContext contextNodeFull = methodExecutionContext.CreateChild<NodeFullContext>();
          contextNodeFull.PerformMeasurementAsync = methodExecutionContext.PerformMeasurementAsync;
          showMessageModels.AddRange(await NodeFullChecker.CheckSequenceAsync(contextNodeFull));

          return showMessageModels;
        }
        await DisconnectPointsToBusAsync(binaryPoints, methodExecutionContext.SchemeModel, step, methodExecutionContext.MessageService);
      }

      return showMessageModels;
    }

    /// <summary>
    /// Возвращает количество разрядов в двоичном представлении наибольшего номера точки в диапазоне.
    /// </summary>
    /// <param name="startPoint">Начальная точка диапазона.</param>
    /// <param name="endPoint">Конечная точка диапазона.</param>
    /// <returns>Количество битов в представлении наибольшего номера точки.</returns>
    static public int GetHighestPointBinaryDigits(List<ChainModel> points)
    {

      var maxPoints = points.Count;
      return Convert.ToString(maxPoints, 2).Length;
    }


    /// <summary>
    /// Преобразует все точки в диапазоне в перевёрнутые двоичные строки фиксированной длины.
    /// </summary>
    /// <param name="first">Первая точка диапазона.</param>
    /// <param name="second">Вторая точка диапазона.</param>
    /// <param name="bitLength">Желаемая длина двоичной строки.</param>
    /// <returns>Список точек и соответствующих перевёрнутых бинарных строк.</returns>
    static public List<(ChainModel point, string reversedBinary)> ConvertToReversedBinaryRange(
        List<ChainModel> points,
        int bitLength)
    {
      if (bitLength <= 0)
      {
        throw new ArgumentOutOfRangeException(nameof(bitLength), "Длина двоичной строки должна быть больше 0.");
      }

      var result = new List<(ChainModel point, string reversedBinary)>();
      string reversPoint = string.Empty;

      for (int i = 1; i <= points.Count; i++)
      {
        var chain = points[i - 1];
        reversPoint = ConvertToReversedBinary(i, bitLength);
        result.Add((chain, reversPoint));
      }

      return result;
    }

    /// <summary>
    /// Преобразует число в двоичную строку заданной длины и переворачивает её.
    /// </summary>
    /// <param name="number">Число для преобразования.</param>
    /// <param name="bitLength">Желаемая длина строки.</param>
    /// <returns>Перевёрнутая двоичная строка.</returns>
    static private string ConvertToReversedBinary(int number, int bitLength)
    {
      string binary = Convert.ToString(number, 2).PadLeft(bitLength, '0');
      char[] array = binary.ToCharArray();
      Array.Reverse(array);
      return new string(array);
    }

    /// <summary>
    /// Подключает все точки группы к соответствующей шине в зависимости от текущего разряда.
    /// </summary>
    static private async Task ConnectPointsToBusAsync(List<(ChainModel point, string reversedBinary)> points, SchemeModel schemeModel, int step, IUserInteractionService messageService)
    {
      foreach (var point in points)
      {
        if (point.reversedBinary[step] == '1')
        {
          await DeviceManager.ConnectChainToBusAAsync(point.point, messageService);
        }
        else
        {
          await DeviceManager.ConnectChainToBusBAsync(point.point, messageService);
        }

      }
    }

    /// <summary>
    /// Отключает все точки группы к соответствующей шине в зависимости от текущего разряда.
    /// </summary>
    static private async Task DisconnectPointsToBusAsync(List<(ChainModel point, string reversedBinary)> points, SchemeModel schemeModel, int step, IUserInteractionService messageService)
    {

      foreach (var point in points)
      {
        if (point.reversedBinary[step] == '1')
        {
          await DeviceManager.DisconnectChainFromBusAAsync(point.point, messageService);
        }
        else
        {
          await DeviceManager.DisconnectChainFromBusBAsync(point.point, messageService);
        }
      }
    }

    /// <summary>
    /// Возвращает строку, в которой только текущий бит равен '1', а остальные — '0'.
    /// </summary>
    /// <param name="step">Текущий шаг (разряд), начиная с младшего.</param>
    /// <returns>Двоичная строка, где установлен только один бит.</returns>
    static public string GetBitString(int step)
    {
      var chars = Enumerable.Repeat('0', HighestBitCount).ToArray();
      chars[HighestBitCount - 1 - step] = '1';
      return new string(chars);
    }

    /// <summary>
    /// Формирует строку фиксированной длины <see cref="HighestBitCount"/>,
    /// состоящую из символов '0' с единственной '1' на позиции,
    /// соответствующей значению <paramref name="number"/> 
    /// (индекс вычисляется как HighestBitCount - number).
    /// </summary>
    /// <param name="number">
    /// Порядковый номер разряда, который должен быть установлен в '1'.
    /// </param>
    /// <returns>
    /// Строка из '0' и одной '1' длиной <see cref="HighestBitCount"/>.
    /// </returns>
    private static string ConvertIntToString(int number)
    {
      var chars = new char[HighestBitCount];
      Array.Fill(chars, '0');

      int index = HighestBitCount - number;

      if (index >= 0 && index < HighestBitCount)
        chars[index] = '1';

      return new string(chars);
    }

  }
}
