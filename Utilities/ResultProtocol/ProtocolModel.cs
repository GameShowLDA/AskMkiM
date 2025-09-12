using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.ResultProtocol
{
  public class ProtocolModel
  {
    /// <summary>
    /// Дата протокола.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Обозначение сборочной единицы.
    /// </summary>
    public string Designation { get; set; }


    /// <summary>
    /// Номер сборочной единицы.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Навзвание программы контроля.
    /// </summary>
    public string Program { get; set; }


    /// <summary>
    /// Время начала выполнения.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Время конца исполнения.
    /// </summary>
    public DateTime EndTime 
    { 
      get; 
      set; 
    }

    /// <summary>
    /// Общее время выполнения.
    /// </summary>
    public TimeSpan ExecutionTime
    {
      get
      {
        return EndTime - StartTime;
      }
    }

    public ProtocolModel()
    {

      Date = DateTime.Now;
      StartTime = DateTime.Now;

      string path = ".\\Resources\\xresume.txt";
      string template =

@"Протокол от $ДАТА
проверки электрических параметров сборочной единицы $ОБОЗНАЧЕНИЕ Зав.N $НОМЕР
Цель проверки: проверка электрических параметров сборочной единицы на соответствие техническим условиям
Оборудование: установка контроля электромонтажа АСК-МКИ
Программа проверки:
  $ПРОГРАММА
  Время начала измерений: $НАЧАЛО
  Время окончания измерений: $КОНЕЦ
  Время выполнения: $ВРЕМЯ

Обрывов: не обнаружено
Замыканий: не обнаружено
Нарушений изоляции: не обнаружено

Заключение: Изделие $ОБОЗНАЧЕНИЕ Зав.N $НОМЕР
            соответствует требованиям КД

Исполнитель ________________________________

Представитель ОК ___________________________

Представитель заказчика (ВП) _______________";

      using (FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
      {

        if (fileStream.Length == 0)
        {
          using (StreamWriter writer = new StreamWriter(fileStream))
          {
            writer.WriteLine(template);
          }
        }
      }
    }


    static public string SetProtocol(ProtocolModel protocolModel)
    {
      string path = ".\\Resources\\xresume.txt";
      string template;
      try
      {
        template = File.ReadAllText(path);
      }
      catch (FileNotFoundException)
      {
        throw new Exception("Файл шаблона не найден");
      }
      catch (IOException ex)
      {
        throw new Exception($"Ошибка чтения файла: {ex.Message}");
      }

      // Формируем финальный текст протокола
      string formattedText = template
          .Replace("$ДАТА", protocolModel.Date.ToString("dd.MM.yyyy"))
          .Replace("$ОБОЗНАЧЕНИЕ", protocolModel.Designation)
          .Replace("$НОМЕР", protocolModel.Number.ToString())
          .Replace("$ПРОГРАММА", protocolModel.Program)
          .Replace("$НАЧАЛО", protocolModel.StartTime.ToString("HH:mm:ss:ff"))
          .Replace("$КОНЕЦ", protocolModel.EndTime.ToString("HH:mm:ss:ff"))
          .Replace("$ВРЕМЯ", protocolModel.ExecutionTime.ToString(@"hh\:mm\:ss\:ff"));

      return formattedText;
    }
  }
}
