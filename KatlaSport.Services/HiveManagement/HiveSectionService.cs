using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using KatlaSport.DataAccess;
using KatlaSport.DataAccess.ProductStoreHive;
using DbHiveSection = KatlaSport.DataAccess.ProductStoreHive.StoreHiveSection;

namespace KatlaSport.Services.HiveManagement
{
    /// <summary>
    /// Represents a hive section service.
    /// </summary>
    public class HiveSectionService : IHiveSectionService
    {
        private readonly IProductStoreHiveContext _context;
        private readonly IUserContext _userContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="HiveSectionService"/> class with specified <see cref="IProductStoreHiveContext"/> and <see cref="IUserContext"/>.
        /// </summary>
        /// <param name="context">A <see cref="IProductStoreHiveContext"/>.</param>
        /// <param name="userContext">A <see cref="IUserContext"/>.</param>
        public HiveSectionService(IProductStoreHiveContext context, IUserContext userContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userContext = userContext ?? throw new ArgumentNullException();
        }

        /// <inheritdoc/>
        public async Task<List<HiveSectionListItem>> GetHiveSectionsAsync()
        {
            var dbHiveSections = await _context.Sections.OrderBy(s => s.Id).ToArrayAsync();
            var hiveSections = dbHiveSections.Select(s => Mapper.Map<HiveSectionListItem>(s)).ToList();
            return hiveSections;
        }

        /// <inheritdoc/>
        public async Task<HiveSection> GetHiveSectionAsync(int hiveSectionId) =>
            Mapper.Map<DbHiveSection, HiveSection>(await GetDbHiveSectionById(hiveSectionId));

        /// <inheritdoc/>
        public async Task<List<HiveSectionListItem>> GetHiveSectionsAsync(int hiveId)
        {
            var dbHiveSections = await _context.Sections.Where(s => s.StoreHiveId == hiveId).OrderBy(s => s.Id).ToArrayAsync();
            var hiveSections = dbHiveSections.Select(s => Mapper.Map<HiveSectionListItem>(s)).ToList();
            return hiveSections;
        }

        /// <inheritdoc />
        public async Task<HiveSection> CreateHiveSectionAsync(UpdateHiveSectionRequest createRequest)
        {
            DbHiveSection[] dbHiveSections = await _context.Sections.Where(h => h.Code == createRequest.Code).ToArrayAsync();
            if (dbHiveSections.Length > 0)
            {
                throw new RequestedResourceHasConflictException($"The hive section with code {createRequest.Code} exists");
            }

            await CheckStoreHiveId(createRequest);

            DbHiveSection dbHiveSection = Mapper.Map<UpdateHiveSectionRequest, DbHiveSection>(createRequest);
            dbHiveSection.CreatedBy = _userContext.UserId;
            dbHiveSection.LastUpdatedBy = _userContext.UserId;
            _context.Sections.Add(dbHiveSection);

            await _context.SaveChangesAsync();

            return Mapper.Map<HiveSection>(dbHiveSection);
        }

        /// <inheritdoc />
        public async Task<HiveSection> UpdateHiveSectionAsync(int hiveSectionId, UpdateHiveSectionRequest updateRequest)
        {
            DbHiveSection[] dbHiveSections = await _context.Sections
                .Where(p => p.Code == updateRequest.Code && p.Id != hiveSectionId).ToArrayAsync();
            if (dbHiveSections.Length > 0)
            {
                throw new RequestedResourceHasConflictException($"The hive section with code {updateRequest.Code} exists");
            }

            await CheckStoreHiveId(updateRequest);

            DbHiveSection dbHiveSection = await GetDbHiveSectionById(hiveSectionId);

            Mapper.Map(updateRequest, dbHiveSection);
            dbHiveSection.LastUpdatedBy = _userContext.UserId;

            await _context.SaveChangesAsync();

            return Mapper.Map<HiveSection>(dbHiveSection);
        }

        /// <inheritdoc />
        public async Task DeleteHiveSectionAsync(int hiveSectionId)
        {
            DbHiveSection dbHiveSection = await GetDbHiveSectionById(hiveSectionId);

            if (dbHiveSection.IsDeleted == false)
            {
                throw new RequestedResourceHasConflictException(
                    $"The hive section with id {hiveSectionId} hasn't got deleted status");
            }

            _context.Sections.Remove(dbHiveSection);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task SetStatusAsync(int hiveSectionId, bool deletedStatus)
        {
            DbHiveSection dbHiveSection = await GetDbHiveSectionById(hiveSectionId);

            if (dbHiveSection.IsDeleted != deletedStatus)
            {
                dbHiveSection.IsDeleted = deletedStatus;
                dbHiveSection.LastUpdated = DateTime.UtcNow;
                dbHiveSection.LastUpdatedBy = _userContext.UserId;

                await _context.SaveChangesAsync();
            }
        }

        private async Task CheckStoreHiveId(UpdateHiveSectionRequest request)
        {
            DbHiveSection[] dbHiveSections = await _context.Sections.Where(h => h.StoreHiveId == request.StoreHiveId).ToArrayAsync();

            if (dbHiveSections.Length == 0)
            {
                throw new RequestedResourceHasConflictException($"The hive with id {request.StoreHiveId} does not exist");
            }
        }

        private async Task<DbHiveSection> GetDbHiveSectionById(int hiveSectionId)
        {
            DbHiveSection[] dbHiveSections = await _context.Sections.Where(s => s.Id == hiveSectionId).ToArrayAsync();

            if (dbHiveSections.Length == 0)
            {
                throw new RequestedResourceNotFoundException();
            }

            return dbHiveSections[0];
        }
    }
}
