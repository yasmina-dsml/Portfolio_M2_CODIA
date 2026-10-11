import json
import logging
import re
from typing import Dict, List
from dataclasses import dataclass

import requests


@dataclass
class ExtractionPrompt:
    SYSTEM_PROMPT = """
Tu es un expert en batteries et électronique. Tu dois extraire des informations précises
depuis des datasheets de batteries au format texte.

RÈGLES IMPORTANTES:
1. Extrais SEULEMENT les informations présentes dans le texte
2. Ne jamais inventer ou supposer des valeurs
3. Si une information n'est pas disponible ou pas claire, marque-la comme "unknown"
4. Respecte les unités trouvées dans le document
5. Préserve la précision des nombres

FORMAT DE SORTIE: JSON uniquement, sans texte supplémentaire.
Respecte exactement les noms de champs demandés. N'ajoute aucun champ non demandé
en dehors de la structure JSON demandée.
"""

    ZERO_SHOT_PROMPT_TEMPLATE = """
Extrait les informations suivantes de cette datasheet de batterie:

INFORMATIONS À EXTRAIRE:
- model: Référence ou modèle exact de la batterie
- manufacturer: Fabricant de la batterie
- chemistry: Chimie de la batterie
- voltage: Tension nominale (V)
- voltage_unit: Unité de la tension nominale
- capacity: Capacité (mAh ou Ah)
- capacity_unit: Unité de capacité
- temperature_range: Plage de température de fonctionnement
- charge_current: Courant de charge standard
- charge_current_unit: Unité du courant de charge
- discharge_current: Courant de décharge standard
- discharge_current_unit: Unité du courant de décharge
- internal_resistance: Résistance interne (mΩ ou Ω)
- internal_resistance_unit: Unité de la résistance interne
- dimensions: Dimensions physiques
- weight: Poids (g ou kg)
- weight_unit: Unité du poids
- cycle_life: Nombre de cycles de vie
- charge_cutoff_voltage: Tension de fin de charge
- discharge_cutoff_voltage: Tension de fin de décharge

CONTRAINTES DE FORMAT:
- Retourne uniquement les champs listés ci-dessus.
- Pour les champs numériques, retourne seulement le nombre, sans unité.
- Mets les unités dans les champs *_unit correspondants.
- Pour dimensions, garde les dimensions utiles avec leurs tolérances si elles sont présentes.
- Si une information n'est pas présente, mets "unknown".
- Si un courant est exprimé en C-rate, convertis-le avec la capacité nominale.
  Exemple: 0.2C pour 120mAh = 24mA.

TEXTE DE LA DATASHEET:
{text}

RÉPONSE (JSON uniquement):
"""

    FEW_SHOT_PROMPT_TEMPLATE = """
Extrait les informations suivantes de cette datasheet de batterie:

INFORMATIONS À EXTRAIRE:
- model: Référence ou modèle exact de la batterie
- manufacturer: Fabricant de la batterie
- chemistry: Chimie de la batterie
- voltage: Tension nominale (V)
- voltage_unit: Unité de la tension nominale
- capacity: Capacité (mAh ou Ah)
- capacity_unit: Unité de capacité
- temperature_range: Plage de température de fonctionnement
- charge_current: Courant de charge standard
- charge_current_unit: Unité du courant de charge
- discharge_current: Courant de décharge standard
- discharge_current_unit: Unité du courant de décharge
- internal_resistance: Résistance interne (mΩ ou Ω)
- internal_resistance_unit: Unité de la résistance interne
- dimensions: Dimensions physiques
- weight: Poids (g ou kg)
- weight_unit: Unité du poids
- cycle_life: Nombre de cycles de vie
- charge_cutoff_voltage: Tension de fin de charge
- discharge_cutoff_voltage: Tension de fin de décharge

CONTRAINTES DE FORMAT:
- Retourne uniquement les champs listés ci-dessus.
- Pour les champs numériques, retourne seulement le nombre, sans unité.
- Mets les unités dans les champs *_unit correspondants.
- Pour dimensions, garde les dimensions utiles avec leurs tolérances si elles sont présentes.
- Si une information n'est pas présente, mets "unknown".
- Si un courant est exprimé en C-rate, convertis-le avec la capacité nominale.
  Exemple: 0.2C pour 120mAh = 24mA.

TEXTE DE LA DATASHEET:
{text}

RÉPONSE (JSON uniquement):
"""

    # Alias conservé pour les tests existants et les appels historiques.
    USER_PROMPT_TEMPLATE = FEW_SHOT_PROMPT_TEMPLATE

    FEW_SHOT_EXAMPLES = [
        {
            "text": (
                "Manufacturer: Example Battery Co., Ltd.\n"
                "Battery Model: EXR14500\n"
                "Battery Type: Lithium-ion rechargeable battery\n"
                "Nominal Voltage: 3.7V\n"
                "Nominal Capacity: 500mAh\n"
                "Internal Impedance: <= 90mΩ\n"
                "Standard Charge: 0.2C\n"
                "Standard Discharge: 0.2C\n"
                "Charging Voltage: 4.20V\n"
                "Discharge Cut-off Voltage: 2.75V\n"
                "Operating Temperature: -20°C to 60°C\n"
                "Dimension: Diameter 14.0 mm (+0.1/-0.1 mm); height 50.0 mm (±0.2 mm)\n"
                "Weight: 18g\n"
                "Cycle life: 500 cycles"
            ),
            "expected": {
                "model": "EXR14500",
                "manufacturer": "Example Battery Co., Ltd.",
                "chemistry": "Lithium-ion rechargeable battery",
                "voltage": 3.7,
                "voltage_unit": "V",
                "capacity": 500,
                "capacity_unit": "mAh",
                "charge_current": 100,
                "charge_current_unit": "mA",
                "discharge_current": 100,
                "discharge_current_unit": "mA",
                "internal_resistance": 90,
                "internal_resistance_unit": "mΩ",
                "temperature_range": "-20°C to 60°C",
                "dimensions": "Diameter 14.0 mm (+0.1/-0.1 mm); height 50.0 mm (±0.2 mm)",
                "weight": 18,
                "weight_unit": "g",
                "cycle_life": 500,
                "charge_cutoff_voltage": 4.2,
                "discharge_cutoff_voltage": 2.75
            }
        }
    ]


class LLMInterface:
    """Interface vers le serveur OpenWebUI (Mistral Unistra)."""

    def __init__(self, config: Dict):
        self.config = config
        self.model = config.get('model', 'chat-mistral')
        self.max_tokens = config.get('max_tokens', 2000)
        self.temperature = config.get('temperature', 0.1)
        self.logger = logging.getLogger(__name__)

        base_url = config.get('base_url', 'https://mai.ia.unistra.fr')
        self.client = {
            'base_url': base_url.rstrip('/'),
            'api_key': config.get('api_key', ''),
            'model': self.model,
        }
        self.logger.info(f"OpenWebUI configuré : {base_url} / modèle : {self.model}")

        self.stats = {'requests': 0, 'total_tokens': 0, 'errors': 0}

    def extract_battery_info(self, text: str, method: str = "zero_shot") -> Dict:
        self.logger.info(f"Extraction LLM démarrée (méthode: {method})")
        try:
            result = self._few_shot_extraction(text) if method == "few_shot" else self._zero_shot_extraction(text)
            self.stats['requests'] += 1
            return result
        except Exception as e:
            self.stats['errors'] += 1
            self.logger.error(f"Erreur extraction LLM: {e}")
            raise

    def _zero_shot_extraction(self, text: str) -> Dict:
        return self._request(ExtractionPrompt.ZERO_SHOT_PROMPT_TEMPLATE.format(text=text))

    def _few_shot_extraction(self, text: str) -> Dict:
        examples_text = "\n\nEXEMPLES:\n"
        for i, example in enumerate(ExtractionPrompt.FEW_SHOT_EXAMPLES):
            examples_text += f"\nExemple {i+1}:\n"
            examples_text += f"Texte: {example['text']}\n"
            examples_text += f"JSON: {json.dumps(example['expected'], indent=2)}\n"
        prompt = examples_text + "\n" + ExtractionPrompt.FEW_SHOT_PROMPT_TEMPLATE.format(text=text)
        return self._request(prompt)

    def _request(self, prompt: str) -> Dict:
        try:
            url = f"{self.client['base_url']}/api/chat/completions"
            headers = {
                "Content-Type": "application/json",
                "Authorization": f"Bearer {self.client['api_key']}"
            }
            payload = {
                "model": self.client['model'],
                "messages": [
                    {"role": "system", "content": ExtractionPrompt.SYSTEM_PROMPT},
                    {"role": "user", "content": prompt}
                ],
                "temperature": self.temperature,
                "max_tokens": self.max_tokens,
                "stream": False
            }
            resp = requests.post(url, headers=headers, json=payload, timeout=60)
            resp.raise_for_status()
            data = resp.json()

            if "choices" in data and data["choices"]:
                content = data["choices"][0]["message"]["content"]
            elif "response" in data:
                content = data["response"]
            else:
                self.logger.error(f"Format réponse inattendu: {data}")
                return self._default_structure()

            self.stats['total_tokens'] += data.get('usage', {}).get('total_tokens', 0)
            return self._parse_response(content)

        except Exception as e:
            self.logger.error(f"Erreur OpenWebUI: {e}")
            raise

    def _parse_response(self, content: str) -> Dict:
        try:
            return json.loads(content)
        except json.JSONDecodeError:
            self.logger.warning("Parsing JSON direct échoué, recherche du JSON...")
            start = content.find('{')
            end = content.rfind('}') + 1
            if start >= 0 and end > start:
                try:
                    return json.loads(content[start:end])
                except json.JSONDecodeError:
                    pass
            self.logger.error(f"Impossible de parser JSON: {content[:200]}...")
            return self._default_structure()

    def _default_structure(self) -> Dict:
        return {field: "unknown" for field in [
            "model", "manufacturer", "chemistry", "voltage", "voltage_unit",
            "capacity", "capacity_unit", "temperature_range",
            "charge_current", "charge_current_unit", "discharge_current",
            "discharge_current_unit", "internal_resistance",
            "internal_resistance_unit", "dimensions", "weight", "weight_unit",
            "cycle_life", "charge_cutoff_voltage", "discharge_cutoff_voltage"
        ]}

    def evaluate_extraction(self, text: str, ground_truth: Dict) -> Dict:
        zero_shot = self.extract_battery_info(text, "zero_shot")
        few_shot = self.extract_battery_info(text, "few_shot")
        zero_metrics = self._calculate_metrics(zero_shot, ground_truth)
        few_metrics = self._calculate_metrics(few_shot, ground_truth)
        return {
            "zero_shot": {"extraction": zero_shot, "metrics": zero_metrics},
            "few_shot": {"extraction": few_shot, "metrics": few_metrics},
            "best_method": "few_shot" if few_metrics["f1"] > zero_metrics["f1"] else "zero_shot",
            "comparison": {
                "zero_shot_f1": zero_metrics["f1"],
                "few_shot_f1": few_metrics["f1"],
                "improvement": few_metrics["f1"] - zero_metrics["f1"]
            }
        }

    # Valeurs considérées comme absentes
    ABSENT_VALUES = {'unknown', 'unkown', 'n/a', 'na', 'none', '', 'null'}

    # Champs numériques avec leur champ unité associé
    NUMERIC_FIELDS = {'voltage', 'capacity', 'charge_current', 'discharge_current',
                      'internal_resistance', 'weight', 'cycle_life',
                      'charge_cutoff_voltage', 'discharge_cutoff_voltage'}

    NUMERIC_TOLERANCE = 0.05

    def _is_missing(self, val) -> bool:
        if val is None:
            return True
        return str(val).strip().lower() in self.ABSENT_VALUES

    def _normalize_to_base(self, value, unit: str) -> float:
        """Normalise une valeur numérique vers l'unité de base.
        Courant → A, Capacité → mAh, Poids → g, Résistance → mΩ
        """
        v = float(str(value).strip())
        u = str(unit).strip().lower() if unit else ''
        if u == 'ma':
            return v / 1000        # mA → A
        if u == 'ah':
            return v * 1000        # Ah → mAh
        if u == 'kg':
            return v * 1000        # kg → g
        if u in ('ω', 'ohm'):
            return v * 1000        # Ω → mΩ
        return v

    def _calculate_metrics(self, predicted: Dict, ground_truth: Dict) -> Dict:
        true_positives = 0
        false_positives = 0
        false_negatives = 0
        per_field: Dict = {}

        for field in set(ground_truth.keys()) | set(predicted.keys()):
            gt_val = ground_truth.get(field)
            pred_val = predicted.get(field)
            gt_missing  = self._is_missing(gt_val)
            pred_missing = self._is_missing(pred_val)

            if gt_missing and pred_missing:
                per_field[field] = {"match": None, "note": "absent des deux côtés"}
            elif gt_missing:
                false_positives += 1
                per_field[field] = {"match": False, "note": "faux positif", "predicted": pred_val}
            elif pred_missing:
                false_negatives += 1
                per_field[field] = {"match": False, "note": "non extrait", "expected": gt_val}
            else:
                matched = self._values_match(field, pred_val, gt_val, predicted, ground_truth)
                if matched:
                    true_positives += 1
                    per_field[field] = {"match": True, "predicted": pred_val, "expected": gt_val}
                else:
                    false_positives += 1
                    false_negatives += 1
                    per_field[field] = {"match": False, "predicted": pred_val,
                                        "expected": gt_val, "note": "valeur incorrecte"}

        precision = true_positives / (true_positives + false_positives) if (true_positives + false_positives) > 0 else 0.0
        recall    = true_positives / (true_positives + false_negatives) if (true_positives + false_negatives) > 0 else 0.0
        f1        = 2 * precision * recall / (precision + recall) if (precision + recall) > 0 else 0.0

        extracted = [f for f, v in predicted.items()   if not self._is_missing(v)]
        expected  = [f for f, v in ground_truth.items() if not self._is_missing(v)]
        coverage  = len(extracted) / len(expected) if expected else 0.0

        return {
            "precision": round(precision, 4),
            "recall":    round(recall, 4),
            "f1":        round(f1, 4),
            "coverage":  round(coverage, 4),
            "true_positives":  true_positives,
            "false_positives": false_positives,
            "false_negatives": false_negatives,
            "per_field": per_field
        }

    def _values_match(self, field: str, predicted, ground_truth,
                      pred_dict: Dict, gt_dict: Dict) -> bool:
        if field in self.NUMERIC_FIELDS:
            try:
                unit_field = field + '_unit'
                p = self._normalize_to_base(predicted,  pred_dict.get(unit_field, ''))
                g = self._normalize_to_base(ground_truth, gt_dict.get(unit_field, ''))
                return p == 0 if g == 0 else abs(p - g) / abs(g) <= self.NUMERIC_TOLERANCE
            except (ValueError, TypeError):
                pass

        pred_norm = self._normalize_text_value(predicted)
        gt_norm   = self._normalize_text_value(ground_truth)

        if pred_norm == gt_norm:
            return True

        if field in {"model", "manufacturer", "chemistry"}:
            if pred_norm in gt_norm or gt_norm in pred_norm:
                return True
            pred_tokens = self._significant_tokens(pred_norm)
            gt_tokens = self._significant_tokens(gt_norm)
            return bool(pred_tokens) and (
                pred_tokens.issubset(gt_tokens) or gt_tokens.issubset(pred_tokens)
            )

        if field == "temperature_range":
            return self._number_sequence(predicted) == self._number_sequence(ground_truth)

        if field == "dimensions":
            pred_numbers = self._number_sequence(predicted)
            gt_numbers   = self._number_sequence(ground_truth)
            return bool(pred_numbers) and all(n in gt_numbers for n in pred_numbers)

        return False

    def _normalize_text_value(self, value) -> str:
        text = str(value).strip().lower()
        text = text.replace("℃", "°c").replace("+", "")
        text = re.sub(r"\s+", " ", text)
        text = re.sub(r"[;,]", "", text)
        return text

    def _number_sequence(self, value) -> List[float]:
        return [round(float(n), 4) for n in re.findall(r"-?\d+(?:\.\d+)?", str(value))]

    def _significant_tokens(self, value: str) -> set:
        tokens = set(re.findall(r"[a-z0-9]+", value.lower()))
        return tokens - {"co", "ltd", "limited", "corp", "company", "rechargeable"}

    def get_stats(self) -> Dict:
        return {
            **self.stats,
            "avg_tokens_per_request": self.stats['total_tokens'] / max(1, self.stats['requests']),
            "error_rate": self.stats['errors'] / max(1, self.stats['requests'])
        }