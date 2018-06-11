
-----------------------------------------------------------------------------------------
-- Composites
-- ProbabilitySelector

-- Select a child to execute based on it's chance to be selected and return Success if it 
-- returns Success, otherwise pick another.\n
-- Returns Failure if no child returns Success or a direct 'Failure Chance' is introduced
-----------------------------------------------------------------------------------------

local ProbabilitySelector = bt.Class("ProbabilitySelector",bt.BTComposite)
bt.ProbabilitySelector = ProbabilitySelector

function ProbabilitySelector:ctor()
    bt.BTComposite.ctor(self)
    self.name = "ProbabilitySelector"
    self.childWeights = {}
    self.failChance = {}
    self.probability = 0
    self.currentProbability = 0
    self.failedIndeces = {}
end

function ProbabilitySelector:init(jsonData)
    if jsonData.childWeights then
        for i=1,#jsonData.childWeights do
            table.insert(self.childWeights,jsonData.childWeights[i]._value)
        end
    end
    if jsonData.failChance ~= nil then
        self.failChance = jsonData.failChance._value
    end
end

function ProbabilitySelector:onExecute(agent,blackboard)
    for i = 1,#self.outConnections do
        while true do
            if self:isContainInFailedIndeces(i) then
                break
            end
            if self.currentProbability > self.childWeights[i] then
                self.currentProbability = self.currentProbability - self.childWeights[i]
                break
            end
            self.status = self.outConnections[i]:execute(agent,blackboard)
            if self.status == bt.Status.Success or self.status == bt.Status.Running then
                return self.status
            end
            if self.status == bt.Status.Failure then
                table.insert(self.failedIndeces,i)
                local newTotal = self:getTotal()
                for k,v in pairs(self.failedIndeces) do
                    newTotal = newTotal - self.childWeights[k]
                end
                self.probability = math.random(0,newTotal)
                return bt.Status.Running
            end
            break
        end
    end
    return bt.Status.Failure
end

function ProbabilitySelector:onGraphStarted()
    self:onReset()
end

function ProbabilitySelector:onReset()
    self.failedIndeces = {}
    self.probability = math.random(0,self:getTotal())
end

function ProbabilitySelector:getTotal()
    local total = self.failChance
    for k,v in pairs(self.childWeights) do
        total = total + v
    end
    return total
end

function ProbabilitySelector:isContainInFailedIndeces(i)
    for k,v in pairs(self.failedIndeces) do
        if v == i then return true end
    end
    return false
end

function ProbabilitySelector:destroy()
    self.childWeights = nil
    self.failChance = nil
    self.failedIndeces = nil
end
