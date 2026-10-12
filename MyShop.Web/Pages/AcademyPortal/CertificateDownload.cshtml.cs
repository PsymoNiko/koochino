using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyShop.Application.Interfaces.Academy;
using MyShop.Application.Interfaces.AdminAccess;
using MyShop.Core.Models.AdminAccess;
using MyShop.Web.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyShop.Web.Pages.AcademyPortal
{
    [Authorize(Roles = "Admin,Instructor,Student")]
    public class CertificateDownloadModel : PageModel
    {
        private readonly IAcademyCertificateService _certs;
        private readonly IAcademyCertificatePdfService _pdf;
        private readonly IAcademyStudentService _students;
        private readonly IAcademyInstructorService _instructors;
        private readonly IAdminPermissionService _perms;

        public CertificateDownloadModel(
            IAcademyCertificateService certs,
            IAcademyCertificatePdfService pdf,
            IAcademyStudentService students,
            IAcademyInstructorService instructors,
            IAdminPermissionService perms)
        {
            _certs = certs;
            _pdf = pdf;
            _students = students;
            _instructors = instructors;
            _perms = perms;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var cert = await _certs.GetByIdAsync(id);
            if (cert == null || !cert.IsIssued)
                return NotFound();

            var user = User;
            var isAdmin = await PermissionCheckHelper.HasAdminModuleAsync(
                user, _perms, "Academy", AdminPermissionActions.Read);
            var idClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? user?.FindFirst("UserId")?.Value;
            var uid = int.TryParse(idClaim, out var parsed) ? parsed : 0;

            if (!isAdmin)
            {
                if (user?.IsInRole("Instructor") == true)
                {
                    var instructor = await _instructors.GetByUserIdAsync(uid);
                    if (instructor == null || instructor.Id != cert.CourseInstructorId)
                        return Forbid();
                }
                else if (user?.IsInRole("Student") == true)
                {
                    var student = await _students.GetByUserIdAsync(uid);
                    if (student == null || student.Id != cert.StudentId)
                        return Forbid();
                }
                else
                {
                    return Forbid();
                }
            }

            var bytes = _pdf.GeneratePdf(cert);
            if (bytes == null || bytes.Length == 0)
                return NotFound();

            var file = SanitizeFile(cert.CertificateNo);
            return File(bytes, "application/pdf", $"certificate-{file}.pdf");
        }

        private static string SanitizeFile(string no)
        {
            var t = (no ?? "cert").Trim();
            var chars = t.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray();
            return chars.Length == 0 ? "cert" : new string(chars);
        }
    }
}
