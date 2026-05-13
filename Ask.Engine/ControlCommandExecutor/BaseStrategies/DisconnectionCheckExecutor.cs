using Ask.Core.Shared.DTO.Protocol;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies
{
  /// <summary>
  /// Универсальный исполнитель проверок на разобщение.
  /// </summary>
  internal static class DisconnectionCheckExecutor
  {

    /// <summary>
    /// Универсальный исполнитель алгоритмов проверки разобщения.
    /// В зависимости от параметров <see cref="DisconnectionCheckRequest"/>:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// выбирает нужный тип алгоритма по <c>AlgorithmKey</c> или флагу альтернативного режима;
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// проверяет наличие соответствующего контекста выполнения;
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// вызывает специализированный <c>Checker</c> для выполнения проверки;
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// возвращает унифицированный результат в виде <see cref="AlgorithmExecutionResult"/>.
    /// </description>
    /// </item>
    /// </list>
    /// Класс инкапсулирует логику маршрутизации алгоритмов и избавляет вызывающий код
    /// от условных операторов и знаний о конкретных реализациях проверок.
    /// </summary>
    internal static async Task<AlgorithmExecutionResult> ExecuteAsync(DisconnectionCheckRequest request)
    {
      if (request == null)
        throw new ArgumentNullException(nameof(request));

      if (request.UseAltPairwiseFirstPoint)
        return await ExecuteAltPairwiseFirstPointAsync(request);

      if (IsNodeFull(request))
      {
        request.NodeFullContext.IsPolarityReversed = false;
        var result = await ExecuteNodeFullAsync(request);
        if (IsInversion(request))
        {
          request.NodeFullContext.IsPolarityReversed = true;
          result.AddRange(await ExecuteNodeFullAsync(request));
        }

        return result;
      }

      if (IsMethodExecution(request))
      {
        request.MethodExecutionContext.IsPolarityReversed = false;
        var result = await ExecuteMethodExecutionAsync(request);
        if (IsInversion(request))
        {
          request.MethodExecutionContext.IsPolarityReversed = true;
          result.AddRange(await ExecuteMethodExecutionAsync(request));
        }

        return result;
      }

      if (IsPairwiseFirstPoint(request))
      {
        request.PairwiseFirstPointContext.IsPolarityReversed = false;
        var result = await ExecutePairwiseFirstPointAsync(request);
        if (IsInversion(request))
        {
          request.PairwiseFirstPointContext.IsPolarityReversed = true;
          result.AddRange(await ExecutePairwiseFirstPointAsync(request));
        }

        return result;
      }

      request.NodeAccumulationContext.IsPolarityReversed = false;
      var resultNodeAccumulation = await ExecuteNodeAccumulationAsync(request);
      if (IsInversion(request))
      {
        request.NodeAccumulationContext.IsPolarityReversed = true;
        resultNodeAccumulation.AddRange(await ExecuteNodeAccumulationAsync(request));
      }
      return resultNodeAccumulation;
    }

    /// <summary>
    /// Определяет, используется ли алгоритм проверки разобщения
    /// для метода полного узла (ключ <c>К</c>).
    /// </summary>
    /// <param name="request">Запрос на выполнение проверки разобщения.</param>
    /// <returns>
    /// <c>true</c>, если <see cref="DisconnectionCheckRequest.AlgorithmKey"/>
    /// содержит ключ метода полного узла; иначе <c>false</c>.
    /// </returns>
    private static bool IsNodeFull(DisconnectionCheckRequest request) =>
      request.AlgorithmKey.Contains("К");

    /// <summary>
    /// Определяет, используется ли алгоритм проверки разобщения
    /// для группового метода (ключ <c>Г</c>).
    /// </summary>
    /// <param name="request">Запрос на выполнение проверки разобщения.</param>
    /// <returns>
    /// <c>true</c>, если <see cref="DisconnectionCheckRequest.AlgorithmKey"/>
    /// содержит ключ группового метода; иначе <c>false</c>.
    /// </returns>
    private static bool IsMethodExecution(DisconnectionCheckRequest request) =>
      request.AlgorithmKey.Contains("Г");

    /// <summary>
    /// Определяет, используется ли алгоритм проверки разобщения
    /// относительно первой точки (ключ <c>Т1</c>).
    /// </summary>
    /// <param name="request">Запрос на выполнение проверки разобщения.</param>
    /// <returns>
    /// <c>true</c>, если <see cref="DisconnectionCheckRequest.AlgorithmKey"/>
    /// содержит ключ проверки относительно первой точки; иначе <c>false</c>.
    /// </returns>
    private static bool IsPairwiseFirstPoint(DisconnectionCheckRequest request) =>
      request.AlgorithmKey.Contains("Т1");

    /// <summary>
    /// Определяет, используется ли алгоритм проверки разобщения
    /// в режиме инверсии (ключ <c>И</c>).
    /// </summary>
    /// <param name="request">Запрос на выполнение проверки разобщения.</param>
    /// <returns>
    /// <c>true</c>, если <see cref="DisconnectionCheckRequest.AlgorithmKey"/>
    /// содержит ключ инверсии; иначе <c>false</c>.
    /// </returns>
    private static bool IsInversion(DisconnectionCheckRequest request) =>
      request.AlgorithmKey.Contains("И");

    /// <summary>
    /// Выполняет альтернативную проверку разобщения
    /// относительно первой точки с использованием альтернативного контекста.
    /// </summary>
    /// <param name="request">
    /// Запрос на выполнение проверки разобщения, содержащий альтернативный контекст
    /// для проверки относительно первой точки.
    /// </param>
    /// <returns>
    /// Результат выполнения алгоритма в виде <see cref="AlgorithmExecutionResult"/>,
    /// содержащий сообщения об ошибках и информационные сообщения.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <c>PairwiseFirstPointAltContext</c> отсутствует.
    /// </exception>
    private static async Task<AlgorithmExecutionResult> ExecuteAltPairwiseFirstPointAsync(
   DisconnectionCheckRequest request)
    {
      if (request.PairwiseFirstPointAltContext == null)
        throw new ArgumentNullException(nameof(request.PairwiseFirstPointAltContext));

      var result =
        await PairwiseFirstPointCheckerAlt.CheckSequenceAsync(
          request.PairwiseFirstPointAltContext);

      return new AlgorithmExecutionResult(result.errorMessage, result.infoMessage);
    }

    /// <summary>
    /// Выполняет проверку разобщения методом полного узла.
    /// </summary>
    /// <param name="request">
    /// Запрос на выполнение проверки разобщения, содержащий контекст метода полного узла.
    /// </param>
    /// <returns>
    /// Результат выполнения алгоритма в виде <see cref="AlgorithmExecutionResult"/>,
    /// содержащий сообщения об ошибках.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <c>NodeFullContext</c> отсутствует.
    /// </exception>
    private static async Task<AlgorithmExecutionResult> ExecuteNodeFullAsync(
      DisconnectionCheckRequest request)
    {
      if (request.NodeFullContext == null)
        throw new ArgumentNullException(nameof(request.NodeFullContext));

      var errors = await NodeFullChecker.CheckSequenceAsync(request.NodeFullContext);
      return AlgorithmExecutionResult.FromErrors(errors);
    }

    /// <summary>
    /// Выполняет проверку разобщения групповым методом.
    /// </summary>
    /// <param name="request">
    /// Запрос на выполнение проверки разобщения, содержащий контекст группового метода.
    /// </param>
    /// <returns>
    /// Результат выполнения алгоритма в виде <see cref="AlgorithmExecutionResult"/>,
    /// содержащий сообщения об ошибках.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <c>MethodExecutionContext</c> отсутствует.
    /// </exception>
    private static async Task<AlgorithmExecutionResult> ExecuteMethodExecutionAsync(
      DisconnectionCheckRequest request)
    {
      if (request.MethodExecutionContext == null)
        throw new ArgumentNullException(nameof(request.MethodExecutionContext));

      var errors =
        await MethodExecutor.CheckSequenceAsync(request.MethodExecutionContext);

      return AlgorithmExecutionResult.FromErrors(errors);
    }

    /// <summary>
    /// Выполняет проверку разобщения относительно первой точки.
    /// </summary>
    /// <param name="request">
    /// Запрос на выполнение проверки разобщения, содержащий контекст
    /// проверки относительно первой точки.
    /// </param>
    /// <returns>
    /// Результат выполнения алгоритма в виде <see cref="AlgorithmExecutionResult"/>,
    /// содержащий сообщения об ошибках.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <c>PairwiseFirstPointContext</c> отсутствует.
    /// </exception>
    private static async Task<AlgorithmExecutionResult> ExecutePairwiseFirstPointAsync(
      DisconnectionCheckRequest request)
    {
      if (request.PairwiseFirstPointContext == null)
        throw new ArgumentNullException(nameof(request.PairwiseFirstPointContext));

      var errors =
        await PairwiseFirstPointChecker.CheckSequenceAsync(
          request.PairwiseFirstPointContext);

      return AlgorithmExecutionResult.FromErrors(errors);
    }

    /// <summary>
    /// Выполняет проверку разобщения методом накапливающего узла.
    /// Используется как алгоритм по умолчанию при отсутствии специальных ключей.
    /// </summary>
    /// <param name="request">
    /// Запрос на выполнение проверки разобщения, содержащий контекст
    /// метода накапливающего узла.
    /// </param>
    /// <returns>
    /// Результат выполнения алгоритма в виде <see cref="AlgorithmExecutionResult"/>,
    /// содержащий сообщения об ошибках.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <c>NodeAccumulationContext</c> отсутствует.
    /// </exception>
    private static async Task<AlgorithmExecutionResult> ExecuteNodeAccumulationAsync(
      DisconnectionCheckRequest request)
    {
      if (request.NodeAccumulationContext == null)
        throw new ArgumentNullException(nameof(request.NodeAccumulationContext));

      var errors =
        await NodeAccumulationChecker.CheckSequenceAsync(
          request.NodeAccumulationContext);

      return AlgorithmExecutionResult.FromErrors(errors);
    }
  }
}
