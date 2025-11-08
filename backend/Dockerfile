# Stage 1: Build
FROM python:3.10-alpine AS builder

WORKDIR /app

RUN apk add --no-cache gcc musl-dev postgresql-dev libpq

RUN python -m venv /app/venv
ENV PATH="/app/venv/bin:$PATH"

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Stage 2: Deploy
FROM python:3.10-alpine

WORKDIR /app

RUN apk add --no-cache libpq

COPY --from=builder /app/venv /app/venv

COPY . .

ENV PATH="/app/venv/bin:$PATH"

EXPOSE 8000

CMD ["sh", "-c", "python manage.py migrate && python manage.py collectstatic --noinput && gunicorn -b 0.0.0.0:8000 core.wsgi:application"]
