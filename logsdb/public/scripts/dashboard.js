let totalLogs = 0;
let currentPage = 0;

function renderPaginationControls() {
    const container = document.getElementById('pagination');
    const limit = parseInt(document.getElementById('pageSize').value);
    const totalPages = Math.ceil(totalLogs / limit);

    let buttons = '';

    // Previous button
    buttons += `<button onclick="currentPage--; fetchLogs()" ${currentPage === 0 ? 'disabled' : ''}>Prev</button>`;

    // Numbered page buttons
    for (let i = 0; i < totalPages; i++) {
        buttons += `<button onclick="currentPage = ${i}; fetchLogs();" ${i === currentPage ? 'style="font-weight:bold"' : ''}>${i}</button>`;
    }

    // Next button
    buttons += `<button onclick="currentPage++; fetchLogs()" ${currentPage > totalPages ? 'disabled' : ''}>Next</button>`;

    const text = `<p>Showing ${(currentPage*limit) + 1} to ${(currentPage+1)*limit} from ${totalLogs} logs</p>`;
    container.innerHTML = buttons + text;
}


async function fetchLogs() {
    const process = document.getElementById('processFilter').value;
    const level = document.getElementById('levelFilter').value;
    const search = document.getElementById('searchBox').value;
    const limit = document.getElementById('pageSize').value;

    const res = await fetch('/logsQuery', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ page: currentPage, limit, search, process, level }),
    });
    const { logs, total } = await res.json();
    totalLogs = total;
    const rows = logs.map(log =>
        `<tr>
            <td>${new Date(parseInt(log.ts))}</td>
            <td>${log.user}</td>
            <td>${log.process}</td>
            <td>${log.level}</td>
            <td>${log.message}</td>
        </tr>`
    ).join('');
    document.getElementById('logTable').innerHTML = rows;

    renderPaginationControls();
}

async function loadFilters() {
    const [processes, levels] = await Promise.all([
        fetch('/filters/processes').then(res => res.json()),
        fetch('/filters/levels').then(res => res.json())
    ]);

    const processSelect = document.getElementById('processFilter');
    processes.forEach(p => {
        const opt = document.createElement('option');
        opt.value = p;
        opt.textContent = p;
        processSelect.appendChild(opt);
    });
    

    const levelSelect = document.getElementById('levelFilter');
    levels.forEach(t => {
        const opt = document.createElement('option');
        opt.value = t;
        opt.textContent = t;
        levelSelect.appendChild(opt);
    });
}

loadFilters();
setInterval(fetchLogs, 5000);

document.getElementById("refresh").addEventListener('click', fetchLogs);
