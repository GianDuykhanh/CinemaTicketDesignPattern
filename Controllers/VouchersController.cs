using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using movieCinema.Data.Services;
using movieCinema.Models;
using System.Threading.Tasks;

namespace movieCinema.Controllers
{
    [Authorize(Roles = "Admin")]
    public class VouchersController : Controller
    {
        private readonly IVouchersService _service;

        public VouchersController(IVouchersService service)
        {
            _service = service;
        }

        // GET: Vouchers
        public async Task<IActionResult> Index()
        {
            var allVouchers = await _service.GetAllAsync();
            return View(allVouchers);
        }

        // GET: Vouchers/Details/1
        public async Task<IActionResult> Details(int id)
        {
            var voucherDetails = await _service.GetByIdAsync(id);
            if (voucherDetails == null) return View("NotFound");

            return View(voucherDetails);
        }

        // GET: Vouchers/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Code,DiscountAmount,DiscountPercentage,IsPercentage,MinOrderAmount,ExpiryDate,IsActive")] Voucher voucher)
        {
            if (!ModelState.IsValid)
            {
                return View(voucher);
            }

            await _service.AddAsync(voucher);
            return RedirectToAction(nameof(Index));
        }

        // GET: Vouchers/Edit/1
        public async Task<IActionResult> Edit(int id)
        {
            var voucherDetails = await _service.GetByIdAsync(id);
            if (voucherDetails == null) return View("NotFound");

            return View(voucherDetails);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Code,DiscountAmount,DiscountPercentage,IsPercentage,MinOrderAmount,ExpiryDate,IsActive")] Voucher voucher)
        {
            if (id != voucher.Id) return View("NotFound");

            if (!ModelState.IsValid)
            {
                return View(voucher);
            }

            await _service.UpdateAsync(id, voucher);
            return RedirectToAction(nameof(Index));
        }

        // GET: Vouchers/Delete/1
        public async Task<IActionResult> Delete(int id)
        {
            var voucherDetails = await _service.GetByIdAsync(id);
            if (voucherDetails == null) return View("NotFound");

            return View(voucherDetails);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var voucherDetails = await _service.GetByIdAsync(id);
            if (voucherDetails == null) return View("NotFound");

            await _service.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
