using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class BuggyController : BaseApiController
    {
        // Test 401 Unauthorized.
        [HttpGet("unauthorized")]
        public ActionResult GetUnauthorized()
        {
            return Unauthorized();
        }

        // Test 403 Forbidden.
        [HttpGet("forbidden")]
        public ActionResult GetForbidden()
        {
            return StatusCode(403);
        }

        // Test 404 Not Found.
        [HttpGet("not-found")]
        public ActionResult GetNotFound()
        {
            return NotFound();
        }

        // Test 550 custom status code.
        [HttpGet("status-550")]
        public ActionResult GetStatus550()
        {
            return StatusCode(550, "Errore personalizzato 550");
        }

        // Test 500 Internal Server Error via exception.
        [HttpGet("server-error")]
        public ActionResult GetServerError()
        {
            throw new InvalidOperationException("Errore 500 generato intenzionalmente");
        }

        // Test 302 Redirect to products list.
        [HttpGet("redirect-products")]
        public ActionResult GetRedirectProducts()
        {
            return Redirect("/api/products");
        }

        // Test 503 Service Unavailable.
        [HttpGet("service-unavailable")]
        public ActionResult GetServiceUnavailable()
        {
            return StatusCode(503, "Servizio non disponibile");
        }

        // Test 400 Validation Error.
        [HttpPost("validationerror")]
        public ActionResult GetValidationError([FromBody] ValidationTestRequest request)
        {
            return ValidationProblem();
        }

        public class ValidationTestRequest
        {
            [Required]
            public string Name { get; set; } = string.Empty;

            [Required]
            public string Email { get; set; } = string.Empty;
        }
    }
}
