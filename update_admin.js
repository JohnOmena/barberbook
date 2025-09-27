const fs = require('fs');
const path = 'BarberBook.Web/Pages/Admin/Index.cshtml';
let text = fs.readFileSync(path, 'utf8');

function ensureReplace(find, replace, label){
  const before = text;
  text = text.replace(find, replace);
  if (text === before) {
    throw new Error(`Failed to replace ${label}`);
  }
}

function rawBlock(strings){
  return String.raw(strings).replace(/\$\{/g, '\\${');
}

const fetchJsonRegex = /    async function fetchJson\([\s\S]*?\}\r?\n\r?\n/;
const fetchJsonBlock = rawBlock`
    async function fetchJson(url, options={}) {
        const res = await fetch(url, { headers: { 'Content-Type':'application/json' }, ...options });
        const txt = await res.text();
        if(!res.ok) throw new Error(txt || `${res.status}`);
        if(!txt) return null;
        try { return JSON.parse(txt); } catch { return null; }
    }

    const formatCurrency = (value) => Number(value ?? 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
    const toKey = (value) => (value ?? '').toString().toLowerCase();
    const removeDiacritics = (value) => {
        try { return (value ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, ''); } catch { return value ?? ''; }
    };

`;
ensureReplace(fetchJsonRegex, fetchJsonBlock, 'fetchJson');

const durationRegex = /    const DURATION_OVERRIDES = \(\(\)=>\{[\s\S]*?return m; \}\)\(\);/;
const durationBlock = rawBlock`    const DURATION_PRESETS = [
        { name: 'Degradê na Zero', minutes: 30 },
        { name: 'Degradê Navalhado (com navalha)', minutes: 40 },
        { name: 'Corte Social (Máquina + Tesoura)', minutes: 20 },
        { name: 'Corte Só Máquina', minutes: 20 },
        { name: 'Corte na Tesoura', minutes: 30 },
        { name: 'Barba', minutes: 20 },
        { name: 'Acabamento (Pezinho) + Sobrancelha', minutes: 10 },
        { name: 'Combo: Cabelo + Barba', minutes: 50 }
    ];
    const DURATION_OVERRIDES = (() => {
        const map = new Map();
        const register = (key, minutes) => {
            if (key && !map.has(key)) { map.set(key, minutes); }
        };
        DURATION_PRESETS.forEach(item => {
            const baseKey = toKey(item.name);
            register(baseKey, item.minutes);
            const strippedKey = removeDiacritics(baseKey);
            if (strippedKey && strippedKey !== baseKey) { register(strippedKey, item.minutes); }
        });
        return map;
    })();`;
ensureReplace(durationRegex, durationBlock, 'duration overrides');

const getDurationRegex = /    function getDurationFor\(serviceId, serviceName\)\{[\s\S]*?return null; \}/;
const getDurationBlock = rawBlock`    function getDurationFor(serviceId, serviceName){
        if(serviceId && SERVICES_BY_ID.has(serviceId)) return SERVICES_BY_ID.get(serviceId).durationMin;
        const baseKey = toKey(serviceName);
        if(DURATION_OVERRIDES.has(baseKey)) return DURATION_OVERRIDES.get(baseKey);
        const strippedKey = removeDiacritics(baseKey);
        if(DURATION_OVERRIDES.has(strippedKey)) return DURATION_OVERRIDES.get(strippedKey);
        if(SERVICES_BY_NAME.has(baseKey)) return SERVICES_BY_NAME.get(baseKey).durationMin;
        if(SERVICES_BY_NAME.has(strippedKey)) return SERVICES_BY_NAME.get(strippedKey).durationMin;
        return null;
    }`;
ensureReplace(getDurationRegex, getDurationBlock, 'getDurationFor');

const loadServicesRegex = /    async function loadServices\(\)\{[\s\S]*?    \}/;
const loadServicesBlock = rawBlock`    async function loadServices(){
        const select = document.getElementById('svc');
        select.innerHTML = '<option>Carregando...</option>';
        const data = await fetchJson(`${apiBase}/api/services`);
        SERVICES_BY_ID.clear();
        SERVICES_BY_NAME.clear();
        data.forEach(s => {
            SERVICES_BY_ID.set(s.id, s);
            SERVICES_BY_NAME.set((s.name || '').toLowerCase(), s);
        });
        select.innerHTML = data.map(s => {
            const priceSuffix = typeof s.price === 'number' ? ` - ${formatCurrency(s.price)}` : '';
            return `<option value="${s.id}">${s.name}${priceSuffix}</option>`;
        }).join('');
        const mSvc = document.getElementById('mSvc');
        mSvc.innerHTML = select.innerHTML;
    }`;
ensureReplace(loadServicesRegex, loadServicesBlock, 'loadServices');

const loadDayRegex = /    async function loadDay\(\)\{[\s\S]*?    \}/;
const loadDayBlock = rawBlock`    async function loadDay(){
        const date = document.getElementById('date').value;
        const data = await fetchJson(`${apiBase}/api/status-dia?date=${date}`);
        const list = document.getElementById('list');
        const now = new Date();
        const html = (data.items || []).map(it => {
            const st = it.status;
            const startDate = new Date(it.startsAt);
            const durationGuess = getDurationFor(null, it.serviceName);
            const endLabel = it.endsAt ? fmtTime(it.endsAt) : (durationGuess ? fmtTime(addMinutesIso(it.startsAt, durationGuess)) : '');
            const priceLabel = typeof it.price === 'number' ? formatCurrency(it.price) : '';
            const infoParts = [];
            if(endLabel) infoParts.push('Término: ' + endLabel);
            if(it.clientContact) infoParts.push('Contato: ' + it.clientContact);
            if(priceLabel) infoParts.push('Preço: ' + priceLabel);
            const infoLine = infoParts.join(' - ');
            const canCheckIn = (st === 'Confirmed' || st === 'Pending');
            const canInService = (st === 'CheckIn');
            const canDone = (st === 'InService');
            const canNoShow = (st === 'Confirmed' && now >= new Date(startDate.getTime() + 15*60000));
            const isFinal = (st === 'Done' || st === 'Cancelled');
            const canDelete = (st === 'Cancelled');
            return `
            <div class='card'>
              <div class='title'>${fmtTime(it.startsAt)} - ${it.serviceName}</div>
              <div class='muted'>${it.clientName || ''}</div>
              <div class='muted'>${infoLine}</div>
              <div style='margin-top:6px'>${statusBadge(it.status)}</div>
              <div class='actions'>
                ${canCheckIn ? `<button class='ghost' onclick="updateStatus('${it.id}','CheckIn')">Confirmar</button>` : ''}
                ${canInService ? `<button class='ghost' onclick="updateStatus('${it.id}','InService')">Iniciar</button>` : ''}
                ${canDone ? `<button class='ghost' onclick="updateStatus('${it.id}','Done')">Finalizar</button>` : ''}
                ${canNoShow ? `<button class='ghost' onclick="updateStatus('${it.id}','NoShow')">No-show</button>` : ''}
                <button class='ghost' onclick="openReschedule('${it.id}','${it.serviceName}','${it.startsAt}')">Remarcar</button>
                <button class='ghost' onclick="openEdit('${it.id}','${it.serviceName}','${it.startsAt}','${(it.clientName||'').replace(/"/g,'&quot;')}')">Editar</button>
                ${!isFinal ? `<button class='ghost' onclick="cancel('${it.id}')">Cancelar</button>` : ''}
                ${canDelete ? `<button class='ghost danger' onclick="deleteAppt('${it.id}')">Excluir</button>` : ''}
              </div>
            </div>`;
        }).join('');
        list.innerHTML = html;
        const totalsEl = document.getElementById('totals');
        if(totalsEl){
            const cashValue = Number(data.cash ?? 0);
            totalsEl.textContent = `Total: ${data.totals ?? 0} | Caixa: ${formatCurrency(cashValue)}`;
        }
    }`;
ensureReplace(loadDayRegex, loadDayBlock, 'loadDay');

fs.writeFileSync(path, text, 'utf8');
console.log('Admin page updated');
