using Ask.Core.Services.Config.Base;

namespace Ask.Engine.UnitTests.Services.Config;

public class LegacyCompatibilityMapperTests
{
  [Fact(DisplayName = "Таблица совместимости: отсутствие соответствия возвращает исходный адрес")]
  public void GetCompatibilityPointByRealAddress_WithoutMapping_ReturnsRealAddress()
  {
    LegacyCompatibilityMapper.SetCompatibilityPointsMap(new());

    var result = LegacyCompatibilityMapper.GetCompatibilityPointByRealAddress("1.2.1");

    Assert.Equal("1.2.1", result);
  }
}
