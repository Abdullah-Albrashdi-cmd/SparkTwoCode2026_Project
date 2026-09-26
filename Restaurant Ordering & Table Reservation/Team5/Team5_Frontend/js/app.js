/* ============================================================
   app.js  —  the ONE shared JavaScript file for the whole site.

   You mostly write HTML. This file gives every page a few small
   helpers so the little script at the bottom of each page stays
   short:
     • api(...)        -> talk to the backend (adds your login token)
     • saveLogin / me  -> remember who is logged in
     • requireLogin()  -> kick guests back to the login page
     • Cart            -> the shopping cart
     • esc(), money()  -> tiny formatting helpers
   You rarely need to change this file.
   ============================================================ */

/* 1) Where is the backend? This project runs on the HTTP launch profile by default. */
const API_BASE = "http://localhost:5073";

/* 2) The address of every backend action we use.
      (The controllers don't all follow the same pattern, so we
       just list the real addresses here once.) */
const EP = {
  login:            "/api/Auth/login",
  register:         "/api/Auth/register",
  users:            "/api/User",
  categories:       "/api/MenuCategory/GetAllMenuCategories",
  categoryCreate:   "/api/MenuCategory/CreateMenuCategory",
  categoryUpdate:   (id) => `/api/MenuCategory/UpdateMenuCategory/${id}`,
  categoryDelete:   (id) => `/api/MenuCategory/DeleteMenuCategory/${id}`,
  items:            "/api/MenuItem/GetAllMenuItems",
  itemCreate:       "/api/MenuItem/CreateMenuItem",
  itemUpdate:       (id) => `/api/MenuItem/UpdateMenuItem/${id}`,
  itemDelete:       (id) => `/api/MenuItem/DeleteMenuItem/${id}`,
  tables:           "/Table/GetAllTables",
  tableCreate:      "/Table/AddTable",
  tableUpdate:      (id) => `/Table/UpdateTable?id=${id}`,
  tableDelete:      (id) => `/Table/RemoveTable?id=${id}`,
  reservations:     "/api/Reservation",
  reservationUpdate:(id) => `/api/Reservation/${id}`,
  reservationStatus:(id, s) => `/api/Reservation/status/${id}?status=${s}`,
  reservationDelete:(id) => `/api/Reservation/${id}`,
  orders:           "/Order/GetAllOrders",
  orderCreate:      "/Order/AddOrder",
  orderStatus:      (id, s) => `/Order/UpdateOrderStatus?id=${id}&status=${s}`,
  orderDelete:      (id) => `/Order/RemoveOrder?id=${id}`,
  orderItems:       "/api/OrderItem",
  orderItemsByOrder:(id) => `/api/OrderItem/filter?orderId=${id}`,
  payments:         "/api/Payment",
  paymentCreate:    "/api/Payment",
  paymentDelete:    (id) => `/api/Payment/${id}`,
  reviews:          "/api/Review",
  reviewCreate:     "/api/Review",
  reviewDelete:     (id) => `/api/Review/${id}`,
  ingredients:      "/api/Ingredient",
  ingredientUpdate: (id) => `/api/Ingredient/${id}`,
  ingredientDelete: (id) => `/api/Ingredient/${id}`,
};

/* 3) Remember the logged-in user (kept in the browser). */
function saveLogin(token, user) {
  localStorage.setItem("spoons_token", token);
  localStorage.setItem("spoons_user", JSON.stringify(user));
}
function me()       { try { return JSON.parse(localStorage.getItem("spoons_user")); } catch { return null; } }
function token()    { return localStorage.getItem("spoons_token"); }
function isLoggedIn(){ return !!token(); }
function isAdmin()  { return me() && me().role === "Admin"; }
function logout()   { localStorage.clear(); location.href = "login.html"; }

/* 4) Guards — put these at the top of a page's script. */
function requireLogin() { if (!isLoggedIn()) location.href = "login.html"; }
function requireAdmin() { if (!isLoggedIn()) location.href = "login.html";
                          else if (!isAdmin()) location.href = "index.html"; }

/* 5) Talk to the backend. Returns the JSON the server sends back.
      Example:  const dishes = await api("GET", EP.items); */
async function api(method, path, body) {
  const options = { method, headers: {} };
  if (body !== undefined) {
    options.headers["Content-Type"] = "application/json";
    options.body = JSON.stringify(body);
  }
  if (token()) options.headers["Authorization"] = "Bearer " + token();

  const res  = await fetch(API_BASE + path, options);
  const text = await res.text();
  let data; try { data = text ? JSON.parse(text) : null; } catch { data = text; }
  if (!res.ok) {
    const msg = (data && (data.message || data.title)) || data || ("Error " + res.status);
    throw new Error(typeof msg === "string" ? msg : JSON.stringify(msg));
  }
  return data;
}

/* 6) Tiny formatting helpers used inside HTML templates. */
function esc(s)   { return String(s ?? "").replace(/[&<>"']/g, c =>
                    ({ "&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#039;" }[c])); }
function money(n) { return "$" + (Number(n) || 0).toFixed(2); }

/* 7) A coloured status label. Just returns a Bootstrap badge. */
function badge(text) {
  const t = String(text || "").toLowerCase();
  let color = "secondary";
  if (/(paid|confirmed|completed|ready|available|in stock)/.test(t)) color = "success";
  else if (/(pending|preparing|reserved|low stock)/.test(t))        color = "warning";
  else if (/(failed|cancelled|occupied|out of stock|inactive)/.test(t)) color = "danger";
  else if (/refunded/.test(t)) color = "info";
  return `<span class="badge text-bg-${color}">${esc(text)}</span>`;
}

/* 8) The shopping cart (saved in the browser). */
const Cart = {
  items() { try { return JSON.parse(localStorage.getItem("spoons_cart")) || []; } catch { return []; } },
  save(list) { localStorage.setItem("spoons_cart", JSON.stringify(list)); },
  add(id, name, price) {
    const list = Cart.items();
    const found = list.find(x => x.id === id);
    if (found) found.qty++;
    else list.push({ id, name, price, qty: 1 });
    Cart.save(list);
  },
  setQty(id, qty) { const list = Cart.items(); const it = list.find(x => x.id === id);
                    if (it) { it.qty = Math.max(1, qty); Cart.save(list); } },
  remove(id) { Cart.save(Cart.items().filter(x => x.id !== id)); },
  clear()    { localStorage.removeItem("spoons_cart"); },
  count()    { return Cart.items().reduce((n, x) => n + x.qty, 0); },
  total()    { return Cart.items().reduce((n, x) => n + x.qty * x.price, 0); },
};

/* 9) When any page loads, show the number of items on the "Cart" button
      (only if that page has a element with id="cartCount"). */
document.addEventListener("DOMContentLoaded", () => {
  const badge = document.getElementById("cartCount");
  if (badge) badge.textContent = Cart.count();
});
