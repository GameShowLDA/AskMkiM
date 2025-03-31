using AppConfig.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Базовый репозиторий для работы с сущностями в базе данных
  /// </summary>
  /// <typeparam name="T">Тип сущности</typeparam>
  public class Repository<T> : ICRUD<T> where T : class
  {
    private readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;

    internal Repository(AppDbContext context)
    {
      _context = context;
      _dbSet = context.Set<T>();
    }

    /// <summary>
    /// Получает все записи из базы данных
    /// </summary>
    public List<T> GetAll()
    {
      return _dbSet.ToList();
    }

    /// <summary>
    /// Получает сущность по идентификатору
    /// </summary>
    public T GetById(int id)
    {
      return _dbSet.Find(id);
    }

    /// <summary>
    /// Создает новую сущность в базе данных
    /// </summary>
    public void Create(T entity)
    {
      _dbSet.Add(entity);
      _context.SaveChanges();
    }

    /// <summary>
    /// Обновляет сущность в базе данных
    /// </summary>
    public void Update(T entity)
    {
      _dbSet.Update(entity);
      _context.SaveChanges();
    }

    /// <summary>
    /// Удаляет сущность по идентификатору
    /// </summary>
    public void Delete(int id)
    {
      var entity = GetById(id);
      if (entity != null)
      {
        _dbSet.Remove(entity);
        _context.SaveChanges();
      }
    }
  }
}
