const fetch = require('node-fetch');
const { encode } = require('gpt-3-encoder');
const matter = require('gray-matter');
const { createClient } = require('@supabase/supabase-js');
require('dotenv').config();

const supabaseUrl = 'https://orwaemnzukohisqxjkpy.supabase.co';
const supabaseKey = process.env.SUPABASE_KEY;
const supabase = createClient(supabaseUrl, supabaseKey);

module.exports = async function (context, req) {
    context.log('JavaScript HTTP trigger function processed a request.');

    // Log environment variables (be careful not to log sensitive information)
    context.log('Environment variables:');
    context.log('GITHUB_OWNER:', process.env.GITHUB_OWNER);
    context.log('GITHUB_REPO:', process.env.GITHUB_REPO);
    context.log('GITHUB_TOKEN:', process.env.GITHUB_TOKEN ? 'Set' : 'Not set');
    context.log('OPENAI_API_KEY:', process.env.OPENAI_API_KEY ? 'Set' : 'Not set');
    context.log('SUPABASE_KEY:', process.env.SUPABASE_KEY ? 'Set' : 'Not set');

    // Get configuration from environment variables or query parameters
    const githubToken = process.env.GITHUB_TOKEN;
    const openaiApiKey = process.env.OPENAI_API_KEY;
    const owner = req.query.owner || process.env.GITHUB_OWNER;
    const repo = req.query.repo || process.env.GITHUB_REPO;
    const timeFrame = req.query.timeFrame || '14 days'; // Default to 1 day if not specified

    context.log('Configuration:');
    context.log('Owner:', owner);
    context.log('Repo:', repo);
    context.log('Time Frame:', timeFrame);

    if (!githubToken || !openaiApiKey || !owner || !repo || !supabaseKey) {
        context.log.error('Missing required parameters or environment variables.');
        context.res = {
            status: 400,
            body: "Missing required parameters or environment variables."
        };
        return;
    }

    try {
        // Fetch recently modified files from GitHub
        context.log('Fetching recently modified files from GitHub...');
        const modifiedFiles = await getRecentlyModifiedFiles(owner, repo, timeFrame, githubToken);
        context.log(`Found ${modifiedFiles.length} recently modified files.`);
        context.log('Modified files:', modifiedFiles);

        let embeddings = [];

        for (const file of modifiedFiles) {
            context.log(`Processing file: ${file.name}`);
            const fileContent = await getFileContent(owner, repo, file.path, githubToken);
            context.log(`File content length: ${fileContent.length} characters`);

            const { data, content } = matter(fileContent);
            context.log('Front matter data:', data);

            const cleanedContent = cleanAndPreprocessContent(content);
            context.log(`Cleaned content length: ${cleanedContent.length} characters`);

            const headerChunks = splitIntoHeaderChunks(cleanedContent, data.title);
            context.log(`Split content into ${headerChunks.length} chunks`);

            context.log('Getting embeddings from OpenAI API...');
            const chunkEmbeddings = await getEmbeddings(headerChunks, openaiApiKey);
            context.log(`Received ${chunkEmbeddings.length} embeddings from OpenAI API`);

            // Extract the middle part of the file path for RuleName
            const ruleName = file.name.split('/')[1];
            context.log(`Extracted rule name: ${ruleName}`);

            for (let i = 0; i < headerChunks.length; i++) {
                embeddings.push({
                    RuleName: ruleName,
                    RuleContent: headerChunks[i],
                    Embeddings: chunkEmbeddings[i]
                });
            }

            context.log(`Processed embeddings for ${ruleName}`);
        }

        context.log(`Total embeddings generated: ${embeddings.length}`);

        // Save embeddings to database
        context.log('Saving embeddings to database...');
        const dbResult = await saveEmbeddingsToDatabase(embeddings);
        context.log('Database operation completed.');

        context.res = {
            status: 200,
            body: JSON.stringify({
                message: `Successfully processed ${embeddings.length} embeddings.`,
                databaseResult: dbResult
            })
        };
    } catch (error) {
        context.log.error('Error processing request:', error);
        context.res = {
            status: 500,
            body: "An error occurred while processing the request."
        };
    }
};

async function saveEmbeddingsToDatabase(embeddings) {
    console.log(`Saving embeddings for ${embeddings.length} chunks to database...`);
    let successCount = 0;
    let errorCount = 0;

    // Group embeddings by rule name
    const embeddingsByRule = embeddings.reduce((acc, embedding) => {
        if (!acc[embedding.RuleName]) {
            acc[embedding.RuleName] = [];
        }
        acc[embedding.RuleName].push(embedding);
        return acc;
    }, {});

    const ruleNames = Object.keys(embeddingsByRule);
    console.log(`Processing ${ruleNames.length} unique rules`);

    for (const ruleName of ruleNames) {
        const ruleEmbeddings = embeddingsByRule[ruleName];
        console.log(`Processing rule: ${ruleName} with ${ruleEmbeddings.length} chunks`);

        try {
            // Delete existing records for this rule
            const { error: deleteError } = await supabase
                .from('rules_test')
                .delete()
                .eq('name', ruleName);

            if (deleteError) {
                throw new Error(`Error deleting existing records for ${ruleName}: ${deleteError.message}`);
            }

            // Insert all new records for this rule
            const { error: insertError } = await supabase
                .from('rules_test')
                .insert(ruleEmbeddings.map(embedding => ({
                    name: embedding.RuleName,
                    content: embedding.RuleContent,
                    embeddings: embedding.Embeddings
                })));

            if (insertError) {
                throw new Error(`Error inserting items for ${ruleName}: ${insertError.message}`);
            }

            successCount += ruleEmbeddings.length;
            console.log(`Successfully processed rule: ${ruleName}`);
        } catch (error) {
            console.error(`Error processing rule ${ruleName}:`, error);
            errorCount += ruleEmbeddings.length;
        }
    }

    console.log('Database operation completed.');
    console.log(`Successfully processed: ${successCount} chunks`);
    console.log(`Failed operations: ${errorCount} chunks`);

    return { successCount, errorCount };
}

async function getRecentlyModifiedFiles(owner, repo, timeFrame, token) {
    const since = new Date(Date.now() - parseDuration(timeFrame)).toISOString();
    const url = `https://api.github.com/repos/${owner}/${repo}/commits?since=${since}`;
    
    console.log(`Fetching commits since ${since}`);
    const response = await fetch(url, {
        headers: { 'Authorization': `token ${token}` }
    });
    const commits = await response.json();
    console.log(`Found ${commits.length} commits`);

    const fileMap = new Map();
    for (const commit of commits) {
        console.log(`Processing commit: ${commit.sha}`);
        const filesUrl = `https://api.github.com/repos/${owner}/${repo}/commits/${commit.sha}`;
        const filesResponse = await fetch(filesUrl, {
            headers: { 'Authorization': `token ${token}` }
        });
        const commitData = await filesResponse.json();
        
        if (commitData.files && Array.isArray(commitData.files)) {
            console.log(`Commit ${commit.sha} modified ${commitData.files.length} files`);
            commitData.files.forEach(file => {
                // Only add or update if the file is in the 'rules' directory and is named 'rule.md'
                if (file.filename.startsWith('rules/') && file.filename.endsWith('/rule.md')) {
                    fileMap.set(file.filename, { name: file.filename, path: file.filename });
                }
            });
        } else {
            console.log(`No files found for commit ${commit.sha}`);
        }
    }

    console.log(`Total unique files modified: ${fileMap.size}`);
    return Array.from(fileMap.values());
}

async function getFileContent(owner, repo, path, token) {
    const url = `https://api.github.com/repos/${owner}/${repo}/contents/${path}`;
    console.log(`Fetching content for file: ${path}`);
    const response = await fetch(url, {
        headers: { 'Authorization': `token ${token}` }
    });
    const data = await response.json();
    const content = Buffer.from(data.content, 'base64').toString('utf-8');
    console.log(`Fetched content length: ${content.length} characters`);
    return content;
}

function cleanAndPreprocessContent(content) {
    console.log('Cleaning and preprocessing content...');
    const cleaned = content
        .replace(/[\r\n]{2,}/g, '\n')
        .replace(/:::\s*(\w+)?([\s\S]*?):::/g, '$2')
        .replace(/<!--\s*end\w+\s*-->/g, '')
        .replace(/\n+/g, "\n")
        .replace("\r\n", "\n")
        .trim();
    console.log(`Content length before: ${content.length}, after: ${cleaned.length}`);
    return cleaned;
}

function splitIntoHeaderChunks(str, title) {
    console.log(`Splitting content for "${title}" into header chunks`);
    const regex = /^###\s+/gm;
    const resultArray = str.split(regex);
    let split = [];

    for (let s of resultArray) {
        const splitResult = splitStringTokens(s, title);
        split.push(...splitResult);
    }
    console.log(`Created ${split.length} header chunks`);
    return split;
}

function splitStringTokens(str, title) {
    const maxChunkLength = 1000;
    let chunks = [str];

    for (let i = 1; encode(chunks[0]).length > maxChunkLength; i++) {
        chunks = splitIntoEqualChunks(str, i);
    }

    chunks = chunks.map(chunk => `# ${title}\n ${chunk}`);
    console.log(`Split string into ${chunks.length} chunks`);
    return chunks;
}

function splitIntoEqualChunks(str, n) {
    const words = str.split(' ');
    const chunkSize = Math.ceil(words.length / n);
    const chunks = [];
    
    for (let i = 0; i < words.length; i += chunkSize) {
        const chunk = words.slice(i, i + chunkSize);
        chunks.push(chunk.join(' '));
    }
    
    console.log(`Split into ${chunks.length} equal chunks`);
    return chunks;
}

async function getEmbeddings(chunks, apiKey) {
    console.log(`Requesting embeddings for ${chunks.length} chunks`);
    const response = await fetch("https://api.openai.com/v1/embeddings", {
        method: 'POST',
        headers: {
            "Content-Type": "application/json",
            "Authorization": `Bearer ${apiKey}`
        },
        body: JSON.stringify({
            "input": chunks,
            "model": "text-embedding-ada-002"
        }),
    });

    const body = await response.json();
    console.log(`Received ${body.data.length} embeddings from OpenAI API`);
    return body.data.map(item => item.embedding);
}

function parseDuration(timeFrame) {
    console.log(`Parsing duration: ${timeFrame}`);
    const [amount, unit] = timeFrame.toLowerCase().split(/\s+/);
    const numericAmount = parseInt(amount, 10);

    if (isNaN(numericAmount)) {
        throw new Error(`Invalid amount in timeFrame: ${timeFrame}`);
    }

    const multipliers = {
        minute: 60 * 1000,
        minutes: 60 * 1000,
        hour: 60 * 60 * 1000,
        hours: 60 * 60 * 1000,
        day: 24 * 60 * 60 * 1000,
        days: 24 * 60 * 60 * 1000,
        week: 7 * 24 * 60 * 60 * 1000,
        weeks: 7 * 24 * 60 * 60 * 1000,
        month: 30 * 24 * 60 * 60 * 1000,
        months: 30 * 24 * 60 * 60 * 1000
    };

    if (!(unit in multipliers)) {
        throw new Error(`Unknown time unit in timeFrame: ${timeFrame}`);
    }

    const duration = numericAmount * multipliers[unit];
    console.log(`Parsed duration: ${duration} milliseconds`);
    return duration;
}