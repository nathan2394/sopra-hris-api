using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using System.Diagnostics;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Entities;
using sopra_hris_api.src.Helpers;
using sopra_hris_api.Helpers;

namespace sopra_hris_api.src.Services
{
    public class UserService : IServiceAsync<Users>
    {
        private readonly EFContext _context;

        public UserService(EFContext context)
        {
            _context = context;
        }

        public async Task<Users> CreateAsync(Users data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(data.Password)) data.Password = "password";
                data.Password = Utility.HashPassword(data.Password);

                await _context.Users.AddAsync(data);
                await _context.SaveChangesAsync();

                await dbTrans.CommitAsync();

                return data;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                await dbTrans.RollbackAsync();

                throw;
            }
        }

        public async Task<bool> DeleteAsync(long id, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Users.FirstOrDefaultAsync(x => x.UserID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = userID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                await dbTrans.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);
                await dbTrans.RollbackAsync();

                throw;
            }
        }

        public async Task<Users> EditAsync(Users data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Users.FirstOrDefaultAsync(x => x.UserID == data.UserID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.RoleID = data.RoleID;

                obj.Name = data.Name;
                obj.Email = data.Email;
                obj.PhoneNumber = data.PhoneNumber;
                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;
                obj.IsVerified = data.IsVerified;
                obj.OTP = data.OTP;
                obj.OtpExpiration = data.OtpExpiration;

                if (!string.IsNullOrEmpty(data.Password))
                    obj.Password = Utility.HashPassword(data.Password);

                await _context.SaveChangesAsync();

                await dbTrans.CommitAsync();

                return obj;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);
                await dbTrans.RollbackAsync();
                throw;
            }
        }

        public async Task<ListResponse<Users>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Users where a.IsDeleted == false select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Name.Contains(search)
                        || x.Email.Contains(search));

                // Filtering
                if (!string.IsNullOrEmpty(filter))
                {
                    var filterList = filter.Split("|", StringSplitOptions.RemoveEmptyEntries);
                    foreach (var f in filterList)
                    {
                        var searchList = f.Split(":", StringSplitOptions.RemoveEmptyEntries);
                        if (searchList.Length == 2)
                        {
                            var fieldName = searchList[0].Trim().ToLower();
                            var value = searchList[1].Trim();
                            query = fieldName switch
                            {
                                "name" => query.Where(x => x.Name.Contains(value)),
                                "email" => query.Where(x => x.Email.Contains(value)),
                                //"role" => query.Where(x => x.RoleID.Contains(value)),
                                _ => query
                            };
                        }
                    }
                }

                // Sorting
                if (!string.IsNullOrEmpty(sort))
                {
                    var temp = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    var orderBy = sort;
                    if (temp.Length > 1)
                        orderBy = temp[0];

                    if (temp.Length > 1)
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderByDescending(x => x.Name),
                            "email" => query.OrderByDescending(x => x.Email),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderBy(x => x.Name),
                            "email" => query.OrderBy(x => x.Email),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.UserID);
                }

                // Get Total Before Limit and Page
                total = await query.CountAsync();

                // Set Limit and Page
                if (limit != 0)
                    query = query.Skip(page * limit).Take(limit);

                // Get Data
                var data = await query.ToListAsync();
                if (data.Count <= 0 && page > 0)
                {
                    page = 0;
                    return await GetAllAsync(limit, page, total, search, sort, filter, date);
                }

                return new ListResponse<Users>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Users> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

    }
}

