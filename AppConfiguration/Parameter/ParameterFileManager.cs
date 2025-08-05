using AppConfiguration.Base;
using AppConfiguration.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.FilesUtility;

namespace AppConfiguration.Parameter
{
  internal class ParameterFileManager : ConfigurationManagerBase<ParameterModel>
  {
    private readonly YamlUtility<ParameterModel> _yamlHelper;


    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ProtocolModel"/> с заданным путем к файлу.
    /// </summary>
    /// <param name="pathFile">Путь к YAML файлу, в котором будет храниться конфигурация модели выполнения.</param>
    internal ParameterFileManager(string pathFile) : base(pathFile)
    {
      _yamlHelper = new YamlUtility<ParameterModel>(pathFile);
    }

    public override async Task<ParameterModel> ReadFileAsync() => await _yamlHelper.ReadAsync();

    public override async Task RewriteFileAsync(ParameterModel data) => await _yamlHelper.RewriteAsync(data);
  }
}
