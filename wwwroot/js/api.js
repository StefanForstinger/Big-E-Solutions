// ── API Helper ────────────────────────────────────────────────────────────────

const API_BASE = '/api';

function getToken() {
  return localStorage.getItem('token');
}

function setToken(token) {
  localStorage.setItem('token', token);
}

function clearToken() {
  localStorage.removeItem('token');
}

function isLoggedIn() {
  return !!getToken();
}

function getUser() {
  const token = getToken();
  if (!token) return null;
  try {
    return JSON.parse(atob(token.split('.')[1]));
  } catch {
    return null;
  }
}

function getUserName() {
  const u = getUser();
  return u?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ?? '';
}

function getUserRole() {
  const u = getUser();
  return u?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? '';
}

function authHeaders() {
  return {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${getToken()}`
  };
}

async function apiFetch(path, options = {}) {
  const res = await fetch(API_BASE + path, {
    headers: authHeaders(),
    ...options
  });

  if (res.status === 401) {
    clearToken();
    window.location.href = '/index.html';
    throw new Error('Nicht eingeloggt');
  }

  return res;
}

// ── UI Helpers ─────────────────────────────────────────────────────────────────

function esc(str) {
  const d = document.createElement('div');
  d.textContent = str ?? '';
  return d.innerHTML;
}

function showAlert(elId, message, type = 'error') {
  const el = document.getElementById(elId);
  if (!el) return;
  el.textContent = message;
  el.className = `alert alert-${type}`;
}

function hideAlert(elId) {
  const el = document.getElementById(elId);
  if (el) el.className = 'alert alert-hidden';
}

function setLoading(btnId, loading) {
  const btn = document.getElementById(btnId);
  if (!btn) return;
  if (loading) {
    btn.dataset.originalText = btn.innerHTML;
    btn.innerHTML = '<span class="spinner"></span>';
    btn.disabled = true;
  } else {
    btn.innerHTML = btn.dataset.originalText ?? btn.innerHTML;
    btn.disabled = false;
  }
}

// ── Modal ────────────────────────────────────────────────────────────────────

function openModal(id) {
  document.getElementById(id)?.classList.add('open');
}

function closeModal(id) {
  document.getElementById(id)?.classList.remove('open');
}
