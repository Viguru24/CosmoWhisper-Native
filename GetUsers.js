import pg from 'pg';
import dotenv from 'dotenv';
dotenv.config();

const { Client } = pg;
const client = new Client({
    connectionString: process.env.DATABASE_URL,
    ssl: { rejectUnauthorized: false }
});

async function run() {
    await client.connect();
    const res = await client.query("SELECT * FROM users WHERE email IN ('louisdesouza@gmail.com', 'willryan2@googlemail.com')");
    console.log(JSON.stringify(res.rows, null, 2));
    await client.end();
}
run().catch(console.error);
