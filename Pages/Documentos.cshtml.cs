using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Asesoria_Legal.Pages
{
    public class DocumentosModel : PageModel
    {
        private readonly IWebHostEnvironment _env;

        private static readonly string[] ExtensionesPermitidas =
            { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
        private const long TamanoMaximo = 5 * 1024 * 1024; 

        public DocumentosModel(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string Usuario { get; set; } = string.Empty;
        public List<string> Archivos { get; set; } = new();

        [BindProperty]
        public IFormFile? Archivo { get; set; }

        [TempData]
        public string? Mensaje { get; set; }

        public IActionResult OnGet()
        {
            if (!ObtenerUsuario()) return RedirectToPage("/Registro");
            CargarArchivos();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ObtenerUsuario()) return RedirectToPage("/Registro");

            if (Archivo == null || Archivo.Length == 0)
            {
                ModelState.AddModelError("Archivo", "Seleccioná un archivo.");
            }
            else
            {
                var extension = Path.GetExtension(Archivo.FileName).ToLowerInvariant();

                if (!ExtensionesPermitidas.Contains(extension))
                    ModelState.AddModelError("Archivo",
                        "Tipo no permitido. Solo: " + string.Join(", ", ExtensionesPermitidas));

                if (Archivo.Length > TamanoMaximo)
                    ModelState.AddModelError("Archivo", "El archivo supera los 5 MB.");
            }

            if (!ModelState.IsValid)
            {
                CargarArchivos();
                return Page();
            }

            
            var original = Path.GetFileNameWithoutExtension(Path.GetFileName(Archivo!.FileName));
            var limpio = string.Concat(original.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
            if (limpio.Length == 0) limpio = "documento";
            if (limpio.Length > 50) limpio = limpio.Substring(0, 50);

            var extensionOk = Path.GetExtension(Archivo.FileName).ToLowerInvariant();
            var nombreFinal = $"{DateTime.Now:yyyyMMddHHmmss}_{limpio}{extensionOk}";

            var carpeta = ObtenerCarpeta();
            Directory.CreateDirectory(carpeta);

            var ruta = Path.Combine(carpeta, nombreFinal);
            using (var stream = new FileStream(ruta, FileMode.Create))
            {
                await Archivo.CopyToAsync(stream);
            }

            Mensaje = "Documento subido correctamente.";
            return RedirectToPage();
        }

        public IActionResult OnGetDescargar(string nombre)
        {
            if (!ObtenerUsuario()) return RedirectToPage("/Registro");

            
            var seguro = Path.GetFileName(nombre);
            var ruta = Path.Combine(ObtenerCarpeta(), seguro);

            if (!System.IO.File.Exists(ruta)) return NotFound();

            return PhysicalFile(ruta, "application/octet-stream", seguro);
        }

        

        private bool ObtenerUsuario()
        {
            var usuario = HttpContext.Session.GetString("Usuario");
            if (string.IsNullOrEmpty(usuario)) return false;
            Usuario = usuario;
            return true;
        }

        private string ObtenerCarpeta()
        {
            var seguro = string.Concat(Usuario.Where(char.IsLetterOrDigit));
            if (seguro.Length == 0) seguro = "usuario";
            return Path.Combine(_env.ContentRootPath, "UserUploads", seguro);
        }

        private void CargarArchivos()
        {
            var carpeta = ObtenerCarpeta();
            if (Directory.Exists(carpeta))
            {
                Archivos = Directory.GetFiles(carpeta)
                                    .Select(Path.GetFileName)
                                    .Where(n => n != null)
                                    .Select(n => n!)
                                    .OrderByDescending(n => n)
                                    .ToList();
            }
        }
    }
}