﻿using System;
using System.Threading.Tasks;
using DbContextScope.Demo.DomainModel;

namespace DbContextScope.Demo.Repositories
{
  public interface IUserRepository
  {
    User Get(Guid userId);

    ValueTask<User> GetAsync(Guid userId);

    void Add(User user);
  }
}
