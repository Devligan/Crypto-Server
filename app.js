const API = 'http://ec2-18-116-170-130.us-east-2.compute.amazonaws.com:5264/api/crypto';

const select = document.getElementById('symbol-select');
const statusEl = document.getElementById('status');
const statsEl = document.getElementById('stats');
const dateInputs = document.getElementById('date-inputs');
const dateFrom = document.getElementById('date-from');
const dateTo = document.getElementById('date-to');

let currentSymbol = null;
let currentRange = 'all';
let showVolume = false;
let allData = [];

// ── Symbol loader ──────────────────────────────────────────
async function loadSymbols() {
  try {
    const res = await fetch(`${API}/symbols`);
    const symbols = await res.json();
    select.innerHTML = symbols.map(s => `<option value="${s}">${s}</option>`).join('');
    statusEl.textContent = `${symbols.length} ASSETS ONLINE`;
    statusEl.className = 'status';
    loadSymbol(symbols[0]);
  } catch {
    select.innerHTML = '<option>ERROR</option>';
    statusEl.textContent = 'CONNECTION FAILED';
    statusEl.className = 'status error';
  }
}

// ── Load full history for a symbol ────────────────────────
async function loadSymbol(symbol) {
  currentSymbol = symbol;
  setChartLoading();

  try {
    const res = await fetch(`${API}/${symbol}`);
    allData = await res.json();
    applyRange();
  } catch {
    setChartError();
  }
}

// ── Apply current range filter and re-render ──────────────
function applyRange() {
  let data = allData;

  if (currentRange === 'custom') {
    const from = dateFrom.value;
    const to = dateTo.value;
    if (from) data = data.filter(d => d.date >= from);
    if (to)   data = data.filter(d => d.date <= to);
  } else if (currentRange !== 'all') {
    const days = parseInt(currentRange);
    data = data.slice(-days);
  }

  if (!data.length) { setChartEmpty(); return; }
  renderStats(data);
  renderChart(data);
  statsEl.style.display = 'grid';
}

// ── Stats bar ──────────────────────────────────────────────
function renderStats(data) {
  const first  = data[0].close;
  const latest = data[data.length - 1].close;
  const change = ((latest - first) / first) * 100;
  const closes  = data.map(d => d.close);
  const volumes = data.map(d => d.volume);
  const totalVol = volumes.reduce((a, b) => a + b, 0);
  const avgVol   = totalVol / volumes.length;

  document.getElementById('stat-latest').textContent = fmt(latest);

  const changeEl = document.getElementById('stat-change');
  changeEl.textContent = (change >= 0 ? '+' : '') + change.toFixed(2) + '%';
  changeEl.className = 'stat-value ' + (change >= 0 ? 'positive' : 'negative');

  document.getElementById('stat-high').textContent   = fmt(Math.max(...closes));
  document.getElementById('stat-low').textContent    = fmt(Math.min(...closes));
  document.getElementById('stat-avgvol').textContent = fmtVol(avgVol);
  document.getElementById('stat-totvol').textContent = fmtVol(totalVol);

  document.getElementById('chart-title').textContent =
    showVolume ? `// VOLUME — ${currentSymbol}` : `// PRICE HISTORY — ${currentSymbol}`;
}

// ── Chart renderer ─────────────────────────────────────────
function renderChart(data) {
  const canvas = document.createElement('canvas');
  canvas.height = 320;
  document.getElementById('chart-area').innerHTML = '';
  document.getElementById('chart-area').appendChild(canvas);

  const ctx = canvas.getContext('2d');
  const W   = canvas.offsetWidth || 900;
  const H   = 320;
  canvas.width = W;

  const values = data.map(d => showVolume ? d.volume : d.close);
  const dates  = data.map(d => d.date);
  const min    = Math.min(...values) * (showVolume ? 0.9 : 0.995);
  const max    = Math.max(...values) * 1.005;
  const pad    = { top: 20, right: 20, bottom: 40, left: 90 };

  const xp = i => pad.left + (i / (values.length - 1)) * (W - pad.left - pad.right);
  const yp = v => pad.top + (1 - (v - min) / (max - min)) * (H - pad.top - pad.bottom);

  ctx.clearRect(0, 0, W, H);

  // Grid + Y labels
  ctx.lineWidth = 1;
  for (let i = 0; i <= 4; i++) {
    const yv  = pad.top + (i / 4) * (H - pad.top - pad.bottom);
    const val = max - (i / 4) * (max - min);
    ctx.strokeStyle = '#00ff4111';
    ctx.beginPath(); ctx.moveTo(pad.left, yv); ctx.lineTo(W - pad.right, yv); ctx.stroke();
    ctx.fillStyle  = '#00aa2a';
    ctx.font       = '10px Courier New';
    ctx.textAlign  = 'right';
    ctx.fillText(showVolume ? fmtVol(val) : fmt(val), pad.left - 6, yv + 4);
  }

  // X labels
  ctx.fillStyle = '#00aa2a';
  ctx.font = '9px Courier New';
  ctx.textAlign = 'center';
  const step = Math.max(1, Math.floor(values.length / 7));
  for (let i = 0; i < values.length; i += step) {
    ctx.fillText(dates[i].slice(5), xp(i), H - 8);
  }

  const color = showVolume ? '#00aaff' : '#00ff41';
  const colorA = showVolume ? '#00aaff22' : '#00ff4122';

  // Fill
  const grad = ctx.createLinearGradient(0, pad.top, 0, H - pad.bottom);
  grad.addColorStop(0, colorA);
  grad.addColorStop(1, 'transparent');
  ctx.beginPath();
  ctx.moveTo(xp(0), yp(values[0]));
  for (let i = 1; i < values.length; i++) ctx.lineTo(xp(i), yp(values[i]));
  ctx.lineTo(xp(values.length - 1), H - pad.bottom);
  ctx.lineTo(xp(0), H - pad.bottom);
  ctx.closePath();
  ctx.fillStyle = grad;
  ctx.fill();

  // Line
  ctx.beginPath();
  ctx.strokeStyle  = color;
  ctx.lineWidth    = 1.5;
  ctx.shadowColor  = color;
  ctx.shadowBlur   = 6;
  ctx.moveTo(xp(0), yp(values[0]));
  for (let i = 1; i < values.length; i++) ctx.lineTo(xp(i), yp(values[i]));
  ctx.stroke();
}

// ── Helpers ────────────────────────────────────────────────
function setChartLoading() {
  document.getElementById('chart-area').innerHTML = '<div class="loading">FETCHING DATA...</div>';
  statsEl.style.display = 'none';
}
function setChartEmpty() {
  document.getElementById('chart-area').innerHTML = '<div class="empty">NO DATA FOR SELECTED RANGE</div>';
  statsEl.style.display = 'none';
}
function setChartError() {
  document.getElementById('chart-area').innerHTML = '<div class="empty">FAILED TO LOAD DATA</div>';
}

function fmt(n) {
  if (n >= 1000) return '$' + n.toLocaleString('en', { maximumFractionDigits: 0 });
  if (n >= 1)    return '$' + n.toFixed(4);
  return '$' + n.toFixed(6);
}
function fmtVol(n) {
  if (n >= 1e9) return '$' + (n / 1e9).toFixed(2) + 'B';
  if (n >= 1e6) return '$' + (n / 1e6).toFixed(2) + 'M';
  return '$' + n.toLocaleString();
}

// ── Event listeners ────────────────────────────────────────
select.addEventListener('change', e => loadSymbol(e.target.value));

document.querySelectorAll('.range-btn').forEach(btn => {
  btn.addEventListener('click', () => {
    document.querySelectorAll('.range-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    currentRange = btn.dataset.range;
    dateInputs.style.display = currentRange === 'custom' ? 'flex' : 'none';
    if (currentRange !== 'custom') applyRange();
  });
});

document.getElementById('apply-range').addEventListener('click', applyRange);

document.getElementById('btn-price').addEventListener('click', () => {
  showVolume = false;
  document.getElementById('btn-price').classList.add('active');
  document.getElementById('btn-volume').classList.remove('active');
  if (allData.length) applyRange();
});

document.getElementById('btn-volume').addEventListener('click', () => {
  showVolume = true;
  document.getElementById('btn-volume').classList.add('active');
  document.getElementById('btn-price').classList.remove('active');
  if (allData.length) applyRange();
});

loadSymbols();