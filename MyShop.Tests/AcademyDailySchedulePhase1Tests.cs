using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Application.Interfaces.Academy;
using MyShop.Application.Services.Academy;
using MyShop.Core.Academy;
using MyShop.Core.Models.Academy;
using Xunit;

namespace MyShop.Tests
{
    public class AcademyDailySchedulePhase1Tests
    {
        [Fact]
        public void Weekly_Schedule_IsNotTreatedAsDaily()
        {
            var weekly = new AcademyCourseScheduleDto
            {
                DayId = 1,
                Hour = 10,
                Minute = 0,
                DurationMinutes = 90,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Weekly
            };

            Assert.True(weekly.IsWeekly);
            Assert.False(weekly.IsDaily);
            Assert.False(AcademyDailyScheduleResolver.TryResolve(weekly, DateTime.Today, out _));
        }

        [Fact]
        public void NullRecurrenceType_DefaultsToWeekly()
        {
            var weekly = new AcademyCourseScheduleDto { DayId = 2, Hour = 9, RecurrenceType = null };
            Assert.True(weekly.IsWeekly);
            Assert.False(weekly.IsDaily);
        }

        [Fact]
        public void Daily_Schedule_RequiresValidDateRange()
        {
            var daily = new AcademyCourseScheduleDto
            {
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                Hour = 20,
                DurationMinutes = 90,
                StartDate = new DateTime(2026, 8, 23),
                EndDate = new DateTime(2027, 8, 17)
            };

            Assert.True(AcademyDailyScheduleResolver.HasValidDailyRange(daily));

            daily.EndDate = new DateTime(2026, 8, 22);
            Assert.False(AcademyDailyScheduleResolver.HasValidDailyRange(daily));

            daily.EndDate = null;
            Assert.False(AcademyDailyScheduleResolver.HasValidDailyRange(daily));
        }

        [Fact]
        public void Daily_OutsideRange_CannotResolve()
        {
            var daily = new AcademyCourseScheduleDto
            {
                Id = 11,
                CourseId = 5,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                Hour = 20,
                Minute = 0,
                DurationMinutes = 90,
                StartDate = new DateTime(2026, 8, 23),
                EndDate = new DateTime(2027, 8, 17)
            };

            Assert.False(AcademyDailyScheduleResolver.TryResolve(daily, new DateTime(2026, 8, 22), out _));
            Assert.False(AcademyDailyScheduleResolver.TryResolve(daily, new DateTime(2027, 8, 18), out _));
        }

        [Fact]
        public void Daily_InsideRange_ResolvesLogicalSession()
        {
            var daily = new AcademyCourseScheduleDto
            {
                Id = 11,
                CourseId = 5,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                Hour = 20,
                Minute = 30,
                DurationMinutes = 90,
                StartDate = new DateTime(2026, 8, 23),
                EndDate = new DateTime(2027, 8, 17)
            };

            Assert.True(AcademyDailyScheduleResolver.TryResolve(daily, new DateTime(2026, 8, 23, 3, 0, 0), out var first));
            Assert.NotNull(first);
            Assert.Equal(11, first.ScheduleId);
            Assert.Equal(5, first.CourseId);
            Assert.Equal(new DateTime(2026, 8, 23), first.SessionDate);
            Assert.Equal(20, first.Hour);
            Assert.Equal(30, first.Minute);
            Assert.Equal(90, first.DurationMinutes);

            Assert.True(AcademyDailyScheduleResolver.TryResolve(daily, new DateTime(2027, 8, 17), out var last));
            Assert.Equal(new DateTime(2027, 8, 17), last.SessionDate);
        }

        [Fact]
        public void ListOccurrences_IgnoresWeekly_AndStaysInsideRange()
        {
            var weekly = new AcademyCourseScheduleDto
            {
                Id = 1,
                DayId = 1,
                Hour = 10,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Weekly
            };
            var daily = new AcademyCourseScheduleDto
            {
                Id = 2,
                CourseId = 5,
                RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                Hour = 20,
                Minute = 0,
                DurationMinutes = 90,
                StartDate = new DateTime(2026, 8, 23),
                EndDate = new DateTime(2026, 8, 25)
            };

            var list = AcademyDailyScheduleResolver.ListOccurrences(
                new[] { weekly, daily },
                new DateTime(2026, 8, 20),
                new DateTime(2026, 8, 30));

            Assert.Equal(3, list.Count);
            Assert.All(list, x => Assert.Equal(2, x.ScheduleId));
            Assert.Equal(new DateTime(2026, 8, 23), list[0].SessionDate);
            Assert.Equal(new DateTime(2026, 8, 25), list[2].SessionDate);
        }

        [Fact]
        public async Task Create_RejectsUnknownRecurrenceType()
        {
            var repo = new FakeCourseRepo { NextId = 42 };
            var svc = new AcademyCourseService(repo);
            var result = await svc.CreateAsync(new CreateAcademyCourseDto
            {
                Title = "ریاضی",
                Schedules = new List<AcademyCourseScheduleDto>
                {
                    new()
                    {
                        RecurrenceType = "Foo",
                        DayId = 1,
                        Hour = 10
                    }
                }
            });

            Assert.False(result.Ok);
            Assert.Equal(0, repo.Inserted);
            Assert.Empty(repo.SavedSchedules);
        }

        [Fact]
        public async Task Create_SavesDailySchedule_WhenRangeIsValid()
        {
            var repo = new FakeCourseRepo { NextId = 40 };
            var svc = new AcademyCourseService(repo);
            var result = await svc.CreateAsync(new CreateAcademyCourseDto
            {
                Title = "ریاضی",
                Schedules = new List<AcademyCourseScheduleDto>
                {
                    new()
                    {
                        RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                        Hour = 20,
                        Minute = 0,
                        DurationMinutes = 90,
                        StartDate = new DateTime(2026, 8, 23),
                        EndDate = new DateTime(2027, 8, 17)
                    }
                }
            });

            Assert.True(result.Ok);
            Assert.Equal(40, result.Id);
            Assert.Single(repo.SavedSchedules);
            Assert.True(repo.SavedSchedules[0].IsDaily);
            Assert.Equal(new DateTime(2026, 8, 23), repo.SavedSchedules[0].StartDate);
            Assert.Equal(new DateTime(2027, 8, 17), repo.SavedSchedules[0].EndDate);
        }

        [Fact]
        public async Task Create_RejectsDaily_WhenEndBeforeStart()
        {
            var repo = new FakeCourseRepo { NextId = 41 };
            var svc = new AcademyCourseService(repo);
            var result = await svc.CreateAsync(new CreateAcademyCourseDto
            {
                Title = "ریاضی",
                Schedules = new List<AcademyCourseScheduleDto>
                {
                    new()
                    {
                        RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                        Hour = 20,
                        StartDate = new DateTime(2026, 8, 23),
                        EndDate = new DateTime(2026, 8, 22)
                    }
                }
            });

            Assert.False(result.Ok);
            Assert.Empty(repo.SavedSchedules);
            Assert.Equal(0, repo.Inserted);
        }

        [Fact]
        public async Task GenerateSessions_UsesOnlyWeekly_AndIgnoresDaily()
        {
            var start = Next(DayOfWeek.Saturday, new DateTime(2026, 8, 22));
            var repo = new FakeCourseRepo
            {
                Course = new AcademyCourseDto
                {
                    Id = 7,
                    Title = "ریاضی",
                    CourseStartDate = start,
                    NumberOfSessions = 2,
                    Schedules = new List<AcademyCourseScheduleDto>
                    {
                        new()
                        {
                            DayId = 1,
                            Hour = 10,
                            Minute = 0,
                            DurationMinutes = 60,
                            RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Weekly
                        },
                        new()
                        {
                            RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                            Hour = 20,
                            DurationMinutes = 90,
                            StartDate = start,
                            EndDate = start.AddDays(400)
                        }
                    }
                }
            };
            var svc = new AcademyCourseService(repo);

            var result = await svc.GenerateSessionsAsync(7, clearExisting: true);

            Assert.True(result.Ok);
            Assert.Equal(2, result.Id);
            Assert.Equal(1, repo.ClearCount);
            Assert.Equal(2, repo.Upserts.Count);
            Assert.All(repo.Upserts, u => Assert.Equal(DayOfWeek.Saturday, u.SessionDate.DayOfWeek));
            Assert.All(repo.Upserts, u => Assert.Equal(new TimeSpan(10, 0, 0), u.StartTime));
        }

        [Fact]
        public async Task GenerateSessions_DailyOnly_DoesNotCreatePhysicalSessions()
        {
            var repo = new FakeCourseRepo
            {
                Course = new AcademyCourseDto
                {
                    Id = 8,
                    Title = "ریاضی",
                    CourseStartDate = new DateTime(2026, 8, 23),
                    NumberOfSessions = 360,
                    Schedules = new List<AcademyCourseScheduleDto>
                    {
                        new()
                        {
                            RecurrenceType = AcademyConstants.ScheduleRecurrenceType.Daily,
                            Hour = 20,
                            DurationMinutes = 90,
                            StartDate = new DateTime(2026, 8, 23),
                            EndDate = new DateTime(2027, 8, 17)
                        }
                    }
                }
            };
            var svc = new AcademyCourseService(repo);

            var result = await svc.GenerateSessionsAsync(8, clearExisting: true);

            Assert.False(result.Ok);
            Assert.Equal(0, repo.ClearCount);
            Assert.Empty(repo.Upserts);
        }

        static DateTime Next(DayOfWeek day, DateTime from)
        {
            var d = from.Date;
            while (d.DayOfWeek != day) d = d.AddDays(1);
            return d;
        }

        sealed class FakeCourseRepo : IAcademyCourseRepository
        {
            public AcademyCourseDto Course { get; set; }
            public int NextId { get; set; } = 1;
            public int Inserted { get; private set; }
            public int ClearCount { get; private set; }
            public List<AcademyCourseScheduleDto> SavedSchedules { get; } = new();
            public List<UpsertAcademyCourseSessionDto> Upserts { get; } = new();

            public Task<List<AcademyCourseDto>> GetAllAsync(int? siteId = null, bool? isActive = null)
                => Task.FromResult(new List<AcademyCourseDto>());

            public Task<AcademyCourseDto> GetByIdAsync(int id)
                => Task.FromResult(Course != null && Course.Id == id ? Course : null);

            public Task<int> InsertAsync(CreateAcademyCourseDto dto)
            {
                Inserted++;
                return Task.FromResult(NextId);
            }

            public Task<bool> UpdateAsync(UpdateAcademyCourseDto dto) => Task.FromResult(true);

            public Task<bool> DeleteAsync(int id) => Task.FromResult(true);

            public Task<List<AcademyCourseScheduleDto>> GetSchedulesAsync(int courseId)
                => Task.FromResult(Course?.Schedules ?? new List<AcademyCourseScheduleDto>());

            public Task<AcademyCourseScheduleDto> GetScheduleByIdAsync(int scheduleId)
                => Task.FromResult(Course?.Schedules?.Find(s => s.Id == scheduleId));

            public Task SetSchedulesAsync(int courseId, IEnumerable<AcademyCourseScheduleDto> schedules)
            {
                SavedSchedules.Clear();
                SavedSchedules.AddRange(schedules ?? Enumerable.Empty<AcademyCourseScheduleDto>());
                return Task.CompletedTask;
            }

            public Task<List<AcademyCourseSessionDto>> GetSessionsAsync(int courseId)
                => Task.FromResult(new List<AcademyCourseSessionDto>());

            public Task ClearSessionsAsync(int courseId)
            {
                ClearCount++;
                return Task.CompletedTask;
            }

            public Task<int> UpsertSessionAsync(UpsertAcademyCourseSessionDto dto)
            {
                Upserts.Add(dto);
                return Task.FromResult(Upserts.Count);
            }

            public Task<bool> DeleteSessionAsync(int id) => Task.FromResult(true);
        }
    }
}
