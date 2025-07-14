const express = require('express');
const sqlite3 = require('sqlite3').verbose();
const bodyParser = require('body-parser');
const app = express();
const db = new sqlite3.Database('events.db');

const PORT = 8080;

app.use(bodyParser.json());

// Initialize DB
db.run(`CREATE TABLE IF NOT EXISTS logs (
  ts TEXT,           -- UNIX timestamp
  user TEXT,         -- Windows username
  process TEXT,      -- Executable or service name
  message TEXT,      -- Log message
  level TEXT         -- INFO, ERROR, DEBUG, etc.
)`);

// # Send logs
// POST endpoint to receive logs
app.post('/log', (req, res) => {
  const { ts, process, level, user, message } = req.body;
  try {
    db.run(`INSERT INTO logs VALUES (?, ?, ?, ?, ?)`, [ts, user, process, message, level]);
    res.sendStatus(200);
  } catch(e) {
    console.error("Error while inserting: ", err);
    res.sendStatus(400);
  }
  
});

// # Serve logs
// Get distinct process names
app.get('/filters/processes', (req, res) => {
  db.all(`SELECT DISTINCT process FROM logs ORDER BY process`, (err, rows) => {
    if (err) {
      return res.status(500).send(err.message);
    }
    res.json(rows.map(r => r.process));
  });
});

// Get distinct log levels
app.get('/filters/levels', (req, res) => {
  db.all(`SELECT DISTINCT level FROM logs ORDER BY level`, (err, rows) => {
    if (err) {
      return res.status(500).send(err.message);
    }
    res.json(rows.map(r => r.level));
  });
});

// GET endpoint to serve logs
app.get('/logs', (req, res) => {
  db.all(`SELECT * FROM logs ORDER BY ts DESC LIMIT 1000`, (err, rows) => {
    if (err) {
      return res.status(500).send(err.message);
    }
    
    res.json(rows);
  });
});

// POST endpoint to query paginated logs
app.post('/logsQuery', (req, res) => {
  console.log('Received request: ', req.body);
  const { page = 0, limit = 100, search = '', process = '', level = '' } = req.body;
  const offset = (page) * limit;

  let baseQuery = `FROM logs WHERE 1=1`;
  const filters = [];

  if (search) {
    baseQuery += ` AND (message LIKE ? OR user LIKE ? OR process LIKE ? OR level LIKE ?)`;
    filters.push(`%${search}%`, `%${search}%`, `%${search}%`, `%${search}%`);
    console.log('Base query after search: ', baseQuery);
  }

  if (process) {
    baseQuery += ` AND (process = ?)`;
    filters.push(`${process}`);
    console.log('Base query after process: ', baseQuery);
  }

  if (level) {
    baseQuery += ` AND (level = ?)`;
    filters.push(`${level}`);
    console.log('Base query after level: ', baseQuery);
  }

  const dataQuery = `SELECT * ${baseQuery} ORDER BY ts DESC LIMIT ? OFFSET ?`;
  const countQuery = `SELECT COUNT(*) as total ${baseQuery}`;

  console.log('Total data query: ', dataQuery);
  console.log('Total count query: ', countQuery);
  console.log('Accumulated filters: ', filters);

  db.get(countQuery, filters, (countErr, rowCount) => {
    if (countErr) {
      console.log('Error in counting logs: ', countErr.message);
      return res.status(500).send(countErr.message);
    }

    db.all(dataQuery, [...filters, limit, offset], (err, rows) => {
      if (err) {
        console.log('Error in querying logs: ', err.message);
        return res.status(500).send(err.message);
      }

      res.json({
        logs: rows,
        total: rowCount.total,
      });
    });
  });
});

const VIEW_PORT = 3031;

// Serve static files from the "public" folder
app.use(express.static('public'));

// Redirect root to dashboard
app.get('/', (req, res) => {
  res.sendFile(__dirname + '/public/dashboard.html');
});

app.listen(PORT, () => console.log(`Log server running at http://localhost:${PORT}`));
