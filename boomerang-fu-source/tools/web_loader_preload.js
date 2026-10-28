	// BF_LOADER_RELIABILITY_V1: bounded fetch/body retries, timeout and cleanup.
	function loadFetch(file, tracker, fileSize, raw) {
		const status = tracker[file] = { total: fileSize || 0, loaded: 0, done: false };
		const abort = new AbortController();
		let idleTimer;
		const stalled = () => abort.abort(new Error(`Download stalled: ${file}. Please reload to retry.`));
		const touch = () => { clearTimeout(idleTimer); idleTimer = setTimeout(stalled, 30000); };
		const deadline = setTimeout(() => abort.abort(new Error(`Download timed out: ${file}. Please reload to retry.`)), 300000);
		touch();
		return fetch(file, { signal: abort.signal }).then(async function (response) {
			if (!response.ok || !response.body) { abort.abort(); throw new Error(`Failed loading file '${file}' (${response.status}). Please reload to retry.`); }
			const reader = response.body.getReader();
			const tracked = new Response(new ReadableStream({
				async pull(controller) {
					try {
						const result = await reader.read();
						if (result.done) { controller.close(); reader.releaseLock(); return; }
						touch();
						status.loaded += result.value.length;
						controller.enqueue(result.value);
					} catch (error) {
						controller.error(error);
						reader.releaseLock();
					}
				},
				cancel(reason) { abort.abort(reason); return reader.cancel(reason); }
			}), { headers: response.headers });
			// Complete the WASM body inside the retry boundary too. The engine's
			// raw response otherwise resolves before a later network failure.
			const bytes = await tracked.arrayBuffer();
			return raw ? new Response(bytes, { headers: response.headers }) : bytes;
		}).finally(function () {
			clearTimeout(idleTimer);
			clearTimeout(deadline);
		});
	}
