using Microsoft.EntityFrameworkCore;

namespace DataBaseConfiguration.Repositories
{
  /// <summary>
  /// Базовый репозиторий для работы с сущностями в базе данных.
  /// </summary>
  /// <typeparam name="T">Тип сущности.</typeparam>
  public class Repository<T> : ICRUD<T> where T : class
  {
    /// <summary>
    /// Контекст базы данных для работы с сущностями.
    /// </summary>
    internal readonly AppDbContext _context;

    /// <summary>
    /// Набор сущностей типа T, используемый для операций с базой данных.
    /// </summary>
    internal readonly DbSet<T> _dbSet;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Repository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных для работы с сущностями.</param>
    internal Repository(AppDbContext context)
    {
      _context = context;
      _dbSet = context.Set<T>();
    }

    /// <inheritdoc />
    public List<T> GetAll()
    {
      return _dbSet.ToList();
    }

    /// <inheritdoc />
    public T GetById(int id)
    {
      return _dbSet.Find(id);
    }

    /// <inheritdoc />
    public void Create(T entity)
    {
      _dbSet.Add(entity);
      _context.SaveChanges();
    }

    /// <inheritdoc />
    public void Update(T entity)
    {
      _dbSet.Update(entity);
      _context.SaveChanges();
    }

    /// <inheritdoc />
    public void Delete(T entity)
    {
      if (entity != null)
      {
        _dbSet.Remove(entity);
        _context.SaveChanges();
      }
    }
  }
}
