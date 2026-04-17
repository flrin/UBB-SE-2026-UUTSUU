using System.Collections.Generic;

namespace SearchAndBook.Repositories;

public interface IRepository<T>
{
    T? GetGameById(int id);
    List<T> GetAllGames();
}