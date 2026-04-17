namespace SearchAndBook.Repositories;

using System.Collections.Generic;
public interface IRepository<T>
{
    T? GetGameById(int id);

    List<T> GetAllGames();
}