using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyShop.Core.Models.Academy;

namespace MyShop.Application.Interfaces.Academy
{
    public interface IAcademyCalendarRepository
    {
        Task<List<AcademyCalendarEventDto>> GetAsync(
            DateTime from, DateTime to, int? instructorId = null, int? studentId = null, string kind = null);
    }

    public interface IAcademyCalendarService
    {
        Task<List<AcademyCalendarEventDto>> GetAsync(
            DateTime from, DateTime to, int? instructorId = null, int? studentId = null, string kind = null);
    }
}
