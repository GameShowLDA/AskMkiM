namespace AppConfig.Enums
{
  public class ValidationEnum
  {
    public enum ValidationDataResult
    {
      Success,
      InvalidPointData,
      ManagerShassyNumberMissing,
      ModuleNotFound,
      PointOutOfRange,
      UniqueError,
      UnknownError,
    }
  }
}
