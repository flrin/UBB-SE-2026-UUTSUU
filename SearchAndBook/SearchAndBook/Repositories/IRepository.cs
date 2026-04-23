namespace SearchAndBook.Repositories;
using System.Collections.Generic;

/// <summary>
/// Interface defining basic repository operations for managing entities of type T, including retrieval by ID and fetching all entities.
/// </summary>
/// <typeparam name="T">The type of entity managed by the repository.</typeparam>
public interface IRepository<T>
{
    /// <summary>
    /// Retrieves an entity of type T by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <returns>The entity of type T if found; otherwise, null.</returns>
    T? GetGameById(int id);

    /// <summary>
    /// Gets a list of all entities of type T.
    /// </summary>
    /// <returns>A list of all entities of type T.</returns>
    List<T> GetAll();
}